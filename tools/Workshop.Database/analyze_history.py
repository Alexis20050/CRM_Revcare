"""Analyze SELECT-only evidence. All arithmetic uses Decimal, no DB access."""
import json
from decimal import Decimal
from pathlib import Path
from collections import Counter

root = Path('artifacts/historical-safety')
def read(name):
    return json.loads((root / (name + '.json')).read_text(encoding='utf-8-sig'), parse_float=Decimal)
def write(name, rows):
    (root / (name + '.json')).write_text(json.dumps(rows, indent=2, default=str), encoding='utf-8')
jobs, tasks, services, estimates, audits, payments = map(read, ['jobs','tasks','services','estimates','audit','payments'])
inquiries, complaints = map(read, ['inquiries','complaints'])
svc = {s['ServiceId']: s for s in services}
est = {e['EstimateNumber']: e for e in estimates}
jobmap = {j['JobOrderId']:j for j in jobs}
taskmap = {j['JobOrderId']:[t for t in tasks if t['JobOrderId']==j['JobOrderId']] for j in jobs}
price_audits = [a for a in audits if 'AgreedPrice' in a['Changes']]
provenance = []
for t in tasks:
    s = svc.get(t['ServiceCatalogId'])
    # Value equality identifies compatibility, never execution provenance or approval.
    classification = 'Unknown: price absent' if t['AgreedPrice'] is None else 'Unverified historical price; catalog-backfill-compatible' if s and s['CurrentPrice']==t['AgreedPrice'] else 'Unverified historical price; differs from current catalog'
    provenance.append(dict(JobTaskId=t['JobTaskId'],JobOrderId=t['JobOrderId'],RecordedAgreedPrice=t['AgreedPrice'],Service=t['ServiceId'],CatalogPrice=s['CurrentPrice'] if s else None,Classification=classification,DocumentedHistoricalAgreement=False,SourceEvidence='LegacyNormalization3NF adds nullable price; previous reconciler assigns catalog CurrentPrice and bypasses audit; no non-null price assignment audit found. Exact row writer/time unknown.',ApprovalEvidence='Unknown'))
assert not any(json.loads(a['Changes']).get('AgreedPrice',{}).get('After') is not None for a in price_audits)
write('task-price-provenance',provenance)
eligible=[]
unresolved=[]
for j in jobs:
    jt=taskmap[j['JobOrderId']]
    total=sum((t['AgreedPrice'] or Decimal(0) for t in jt),Decimal(0))
    missing=sum(t['AgreedPrice'] is None for t in jt)
    # Reproduce prior algorithm, including the catalog fallback used in dry run.
    projected=sum((t['AgreedPrice'] if t['AgreedPrice'] is not None else svc[t['ServiceCatalogId']]['CurrentPrice'] if t['ServiceCatalogId'] in svc else next((s['CurrentPrice'] for s in services if s['Name']==t['ServiceId']),Decimal(0)) for t in jt),Decimal(0))
    e=est.get(f"EST-{j['JobOrderId']:06}")
    reused=bool(e and e['CustomerId']==j['CustomerId'] and e['VehicleId']==j['VehicleId'] and e['TotalAmount']==j['TotalEstimatedCost'])
    if j['ApprovedEstimateId'] is None and (reused or projected==j['TotalEstimatedCost']):
        eligible.append(dict(JobOrderId=j['JobOrderId'],Customer=j['Customer'],AuthoritativeTotal=j['TotalEstimatedCost'],RecordedTaskTotal=total,MissingPrices=missing,PreviouslyProjectedTaskTotal=projected,PreviousReason='Generated reference metadata match' if reused else 'Projected/catalog task total matches',ExistingEstimate=e,Classification='Not eligible: historical agreement/approval unverified',Action='Preserve original amount and references; request original approved quotation and line-price evidence; no automatic linking',ManualBusinessReview=True))
    if j['CustomerEmail'].endswith('@revcare-seed.example'):
        seed=[a['AuditEntryId'] for a in audits if a['EntityType']=='JobOrder' and a['Actor']=='legacy-fictional-20261123-v1' and json.loads(a['Changes']).get('CustomerId',{}).get('After')==j['CustomerId'] and a['Action']=='Added']
        assert j['CustomerEmail'].endswith('@revcare-seed.example') and len(seed)==1
        source=next(a for a in audits if a['AuditEntryId']==seed[0])
        assert Decimal(str(json.loads(source['Changes'])['TotalEstimatedCost']['After']))==j['TotalEstimatedCost']
        unresolved.append(dict(JobOrderId=j['JobOrderId'],Customer=j['Customer'],Status=j['Status'],AuthoritativeTotal=j['TotalEstimatedCost'],RecordedTaskTotal=total,MissingTaskPrices=missing,ExactDifferenceAuthoritativeMinusRecorded=Decimal(j['TotalEstimatedCost'])-total,PreviouslyProjectedCatalogTaskTotal=projected,HistoricalEstimateReference=j['EstimateId'] or 'Unknown / absent',SourceOfTaskPrices='Unknown (null); former dry run substituted catalog prices' if missing else 'Unverified / catalog-backfill-compatible; no row-level provenance',Tasks=jt,SeedAuditEntryIds=seed,AuthoritativeAmountSource='Fictional batch legacy-fictional-20261123-v1; LegacySeed.cs prices[]; per-customer Added audit confirms amount',SupportingParts='No separately priced supporting document found in exported records',SupportingSupplies='No separately priced supporting document found in exported records',SupportingDiscounts='No separately priced supporting document found in exported records',SupportingFees='No separately priced supporting document found in exported records',SupportingOtherCharges='Unknown; task/intake narrative is not an agreed charge',RecommendedAction='Preserve original authoritative total. Business owner must decide treatment of fictional seed records; obtain approved itemization if retained. Do not invent charges or approval.',ManualBusinessReview=True))
for r in unresolved:
    r['SupportingIntakeRecords']=[dict(InquiryId=i['InquiryId'],MessageDetails=i['MessageDetails']) for i in inquiries if i.get('ConvertedJobOrderId')==r['JobOrderId']]
    r['SupportingComplaintRecords']=[dict(ComplaintId=c['ComplaintId'],Description=c['Description'],ActionTaken=c['ActionTaken']) for c in complaints if c.get('JobOrderId')==r['JobOrderId']]
assert len(unresolved)==200 and {x['JobOrderId'] for x in unresolved}==set(range(572,772))
assert len(eligible)==47
write('previously-eligible-47',eligible)
write('actual-unresolved-572-771',unresolved)
requested={r['JobOrderId']:r for r in unresolved}
write('requested-542-741',[requested.get(i,dict(JobOrderId=i,Customer='UNKNOWN: absent from current db80735',Status='Absent',AuthoritativeTotal=None,RecordedTaskTotal=None,ExactDifference=None,HistoricalEstimateReference='Unknown',SourceOfTaskPrices='Unknown',SupportingCharges='Unknown',RecommendedAction='Locate prior snapshot or correct ID range; do not invent record',ManualBusinessReview=True)) for i in range(542,742)])
review=[]
for p in payments:
    if p['PaymentType'] not in ('Insurer Share','Final Balance'): continue
    jp=[x for x in payments if x['JobOrderId']==p['JobOrderId'] and not x['IsVoided']]
    paid=sum((x['AmountPaid'] for x in jp),Decimal(0))
    earlier=sum((x['AmountPaid'] for x in jp if (x['CreatedAt'],x['PaymentId'])<(p['CreatedAt'],p['PaymentId'])),Decimal(0))
    review.append(dict(PaymentId=p['PaymentId'],JobOrderId=p['JobOrderId'],Label=p['PaymentType'],Amount=p['AmountPaid'],StoredPayer=p['PayerType'],StoredPurpose=p['PaymentPurpose'],PayerConclusion='Unknown actual remitter; label suggests Insurance but payer identity requires remittance/receipt evidence' if p['PaymentType']=='Insurer Share' else 'Unknown actual remitter; Final Balance does not identify payer',PurposeConclusion='Unknown: Insurer Share describes allocation, not Deposit/Progress/Settlement' if p['PaymentType']=='Insurer Share' else 'Settlement supported by final-balance label and UI/seeder semantics; verify chronological ledger and original receipt before applying',JobAuthoritativeTotal=jobmap[p['JobOrderId']]['TotalEstimatedCost'],EarlierNonvoidPayments=earlier,CurrentNonvoidPayments=paid,BalanceAfterThisReceipt=Decimal(jobmap[p['JobOrderId']]['TotalEstimatedCost'])-earlier-p['AmountPaid'],ManualBusinessReview=True,Action='No reclassification performed'))
assert len(review)==17
write('payment-review-17',review)
text=['REVCARE HISTORICAL RECORD REVIEW — 2026-11-24 Asia/Manila', 'Difference = authoritative total minus recorded non-null task prices. Null prices remain UNKNOWN, not zero-valued agreements.', 'A catalog projection is displayed separately and is not recorded historical pricing.', '']
for r in unresolved:
    text += [f"Job #{r['JobOrderId']} | {r['Customer']} | {r['Status']}",f"Authoritative: {r['AuthoritativeTotal']:.2f}; recorded tasks: {r['RecordedTaskTotal']:.2f}; exact difference: {r['ExactDifferenceAuthoritativeMinusRecorded']:.2f}; missing prices: {r['MissingTaskPrices']}",f"Historical estimate: {r['HistoricalEstimateReference']}; former catalog projection: {r['PreviouslyProjectedCatalogTaskTotal']:.2f}",f"Task price source: {r['SourceOfTaskPrices']}",f"Job amount source: {r['AuthoritativeAmountSource']}; audit IDs: {r['SeedAuditEntryIds']}", 'Parts/supplies/discounts/fees/other charges: no separately priced supporting document found; any external documentation UNKNOWN.', f"Action: {r['RecommendedAction']} Manual business review: YES.",'']
(root/'actual-unresolved-572-771.txt').write_text('\n'.join(text),encoding='utf-8')
(root/'requested-542-741.txt').write_text('Requested IDs #542-#741: #542-#571 are ABSENT from current db80735. Customer, status, totals, differences, estimate reference, task price source and supporting charges are UNKNOWN for each absent record. Manual business review required to locate prior snapshot or correct IDs.\n\n'+'\n'.join(f'Job #{i}: ABSENT — all historical fields unknown; manual review YES.' for i in range(542,572))+'\n\n'+'\n'.join(text[:4]+text[4:4+170*8]),encoding='utf-8')
summary=dict(Jobs=len(jobs),Tasks=len(tasks),PriceClassifications=dict(Counter(p['Classification'] for p in provenance)),PreviouslyEligible=len(eligible),Unresolved=len(unresolved),PaymentReview=len(review),ExistingBackfilledEstimates=sum('backfilled' in e['Notes'].lower() for e in estimates),AuthoritativeUnresolvedSum=sum(Decimal(r['AuthoritativeTotal']) for r in unresolved),RecordedUnresolvedTaskSum=sum(r['RecordedTaskTotal'] for r in unresolved),NonvoidPaymentSum=sum(p['AmountPaid'] for p in payments if not p['IsVoided']))
write('summary',summary)
print(json.dumps(summary,indent=2,default=str))

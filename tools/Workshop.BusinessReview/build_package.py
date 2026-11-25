"""Offline business-review package. Reads evidence; never connects to a database.
Use a new output directory on each run; existing files are never overwritten.
Amounts are Decimal and serialize as exact two-decimal strings.
"""
import argparse
import hashlib
import html
import json
import re
from collections import Counter
from decimal import Decimal
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SAFE = ROOT / 'artifacts/historical-safety'
COMP = ROOT / 'artifacts/historical-compatibility'
inputs = {}

def load(path):
    inputs[str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
    return json.loads(path.read_text(encoding='utf-8-sig'), parse_float=Decimal)

def money(n):
    return 'Unknown' if n is None else f'{n:,.2f}'

def serialize(value):
    if isinstance(value, Decimal): return f'{value:.2f}'
    raise TypeError(type(value).__name__)

def h(value): return html.escape(str(value))

jobs = load(COMP / 'cloud-keys.json')
estimates = load(COMP / 'cloud-estimates.json')
lines = load(COMP / 'cloud-estimate-lines.json')
audits = load(COMP / 'cloud-audit.json')
tasks = load(SAFE / 'tasks.json')
services = load(SAFE / 'services.json')
payments = load(SAFE / 'payments.json')
customers = load(SAFE / 'customers.json')
prior_prices = load(SAFE / 'task-price-provenance.json')
key_map = load(COMP / 'seed-key-map-local-cloud.json')
prior_estimates = load(COMP / 'estimate-review-154.json')
for file in ('IMPLEMENTATION_READINESS.txt', 'estimate-review-154.txt', 'missing-542-571-investigation.txt', 'production-tests.log', 'business-rules.log'):
    path = COMP / file
    inputs[str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
jm = {j['JobOrderId']: j for j in jobs}
cm = {c['CustomerId']: c for c in customers}
sm = {s['ServiceId']: s for s in services}
parsed_audits = []
for a in audits:
    try: changes = json.loads(a['Changes'], parse_float=Decimal)
    except ValueError: continue
    parsed_audits.append((a, changes))

# Link by stored FK first. A generated-number/backfill-note association is only a
# source-record association and NEVER treated as approval or as an existing link.
estimate_register = []
for e in estimates:
    linked = [j for j in jobs if j['ApprovedEstimateId'] == e['EstimateId']]
    match = re.search(r'Job Order #(\d+)', e['Notes'])
    source = jm.get(int(match.group(1))) if match else None
    if len(linked) == 1:
        job = linked[0]
        assert source is None or source['JobOrderId'] == job['JobOrderId']
        association = 'Stored ApprovedEstimateId FK; approval evidence still unverified'
    elif not linked and source:
        job = source
        association = 'Backfill note identifies source job; NO stored job-estimate link'
    else:
        raise AssertionError('Ambiguous estimate/job association needs manual investigation')
    el = [l for l in lines if l['EstimateId'] == e['EstimateId']]
    total = sum((l['LineTotal'] for l in el), Decimal(0))
    authoritative = job['TotalEstimatedCost']
    difference = None if authoritative is None else authoritative - total
    repeats = Counter((l['ServiceCatalogId'], l['Description'], l['Quantity'], l['UnitPrice'], l['LineTotal']) for l in el)
    repeated_ids = [l['EstimateLineId'] for l in el if repeats[(l['ServiceCatalogId'],l['Description'],l['Quantity'],l['UnitPrice'],l['LineTotal'])] > 1]
    assert 'backfilled' in e['Notes'].lower()
    estimate_register.append(dict(
        EstimateId=e['EstimateId'], EstimateReference=e['EstimateNumber'], JobOrderId=job['JobOrderId'],
        Customer=job['Customer'], JobAssociation=association, LinkedJobIds=[j['JobOrderId'] for j in linked],
        AuthoritativeJobTotal=authoritative, StoredEstimateTotal=e['TotalAmount'], ExistingItemizedTotal=total,
        DifferenceJobMinusItemized=difference, DifferenceEstimateMinusItemized=e['TotalAmount']-total,
        EstimateTotalMatchesJob=e['TotalAmount']==authoritative,
        HistoricalEvidenceClass='Reconstructed/backfilled — documented by stored Notes',
        HistoricalAgreementVerified=False, ApprovalProvenance='Unknown; no original approval evidence in supplied records',
        StoredStatus=e['Status'], StoredApprovedAt=e['ApprovedAt'], JobStartedAt=job['StartedAt'],
        ApprovalTimeEqualsJobStart=e['ApprovedAt']==job['StartedAt'],
        FinancialConsistency='Itemization discrepancy' if difference else 'Arithmetic consistent only; agreement unverified',
        RepeatedIdenticalLineIds=repeated_ids, RepeatedLinesConclusion='Review only; repeated descriptions/prices do not prove duplicate charges',
        LineArithmeticValid=all(l['Quantity']*l['UnitPrice']==l['LineTotal'] for l in el),
        HistoricalReferenceText=job['EstimateId'] or None, BackfillNote=e['Notes'], ExistingLines=el,
        AvailableEvidence=[f'cloud-keys.json: JobOrderId={job["JobOrderId"]}', f'cloud-estimates.json: EstimateId={e["EstimateId"]}', 'cloud-estimate-lines.json: listed EstimateLineIds', 'Stored legacy reference text is a locator, not an original quotation document'],
        ReviewPriority='Financial discrepancy + approval provenance' if difference else 'Approval provenance',
        HumanReviewRequired=True, ReviewStatus='Not reviewed by business', Reviewer=None, Decision=None,
        RequestedEvidence='Original dated quotation/job authorization; line-level amounts and approval evidence. If unavailable, retain unresolved legacy classification.',
        ProposedAction='Qualify reconstruction; preserve original job total, receipts, estimate status and timestamps pending separately approved correction. Never add balancing charges.'))
discrepancies = [r for r in estimate_register if r['DifferenceJobMinusItemized'] != 0]
assert len(estimate_register)==154 and len(discrepancies)==31
assert [r['EstimateId'] for r in discrepancies]==list(range(1,31))+[32]
assert all(r['EstimateTotalMatchesJob'] and r['LineArithmeticValid'] for r in estimate_register)

task_register = []
for t in tasks:
    job = jm[t['JobOrderId']]
    svc = sm.get(t['ServiceCatalogId'])
    price_events = []
    for a,ch in parsed_audits:
        if a['EntityType']!='JobTask' or not isinstance(ch.get('AgreedPrice'),dict): continue
        # Positive saved identity only; temporary negative EF keys cannot identify a task.
        ids = ch.get('JobTaskId',{})
        if isinstance(ids,dict) and t['JobTaskId'] in ids.values(): price_events.append(a['AuditEntryId'])
    matches = svc is not None and t['AgreedPrice'] == svc['CurrentPrice']
    classification = 'Missing / unknown' if t['AgreedPrice'] is None else 'Catalog-compatible; row-level source unverified' if matches else 'Other unverified recorded price'
    task_register.append(dict(
        JobTaskId=t['JobTaskId'], JobOrderId=t['JobOrderId'], Customer=job['Customer'], Service=t['ServiceId'],
        RecordedAgreedPrice=t['AgreedPrice'], ServiceCatalogId=t['ServiceCatalogId'], CurrentCatalogPrice=svc['CurrentPrice'] if svc else None,
        MatchesCurrentCatalog=matches, SourceClass=classification, SourceSupportedHistoricalAgreement=False,
        ConfirmedCatalogAssignment=False, PriceAssignmentAuditIds=price_events,
        KnownMechanism='Archived reconciler assigns missing AgreedPrice from catalog CurrentPrice while bypassing auditing',
        RowLevelOrigin='Unknown: catalog equality proves neither assignment path nor customer agreement',
        AvailableEvidence=[f'tasks.json: JobTaskId={t["JobTaskId"]}', f'services.json: ServiceId={t["ServiceCatalogId"]}', 'Legacy3NfReconciliationService.before.txt', 'cloud-audit.json: no non-null AgreedPrice assignment evidence'],
        SeedJob=job['CustomerEmail'].endswith('@revcare-seed.example'),
        SeedEvidenceLimit='Seed job totals have separate provenance; that does not establish task-price agreement',
        HumanReviewRequired=True, ReviewStatus='Not reviewed by business', Reviewer=None, Decision=None,
        RequestedEvidence='Original agreed service amount or approved quotation line with reliable task/job association',
        ProposedAction='Preserve recorded price; do not use it to replace authoritative job amount or reconstruct approval.'))
assert len(task_register)==603 and len(prior_prices)==603
assert all(r['MatchesCurrentCatalog'] and not r['PriceAssignmentAuditIds'] for r in task_register)
assert not any(isinstance(ch.get('AgreedPrice'),dict) and ch['AgreedPrice'].get('After') is not None for _,ch in parsed_audits)

missing_register = []
for jid in range(542,572):
    events = []
    for a,ch in parsed_audits:
        matching=[]
        for field in ('JobOrderId','ConvertedJobOrderId','LinkedJobOrderId'):
            values=ch.get(field,{})
            if isinstance(values,dict):
                for side,value in values.items():
                    if isinstance(value,int) and value==jid: matching.append(f'{field}.{side}={jid}')
        if matching:
            events.append(dict(AuditEntryId=a['AuditEntryId'],OccurredAtUtc=a['OccurredAtUtc'],Actor=a['Actor'],EntityType=a['EntityType'],Action=a['Action'],MatchedFields=matching,OriginalChanges=ch))
    assert jid not in jm
    missing_register.append(dict(JobOrderId=jid,PresentInExport=False,AuditEventCount=len(events),
        AuditEvents=events,FirstEventUtc=min((e['OccurredAtUtc'] for e in events),default=None),LastEventUtc=max((e['OccurredAtUtc'] for e in events),default=None),
        EvidenceSupportedFact='Positive job key referenced by surviving audit entries; job absent from current export' if events else 'Absent from export; no direct positive-key audit references found',
        ConfirmedDeletion=False,AbsenceCause='Unknown',
        TestCleanupHypothesis='Pattern consistent with repeated test activity: six groups of five IDs, 13 Added events per group; test source contains cleanup SQL. No per-key deletion proof.',
        HumanReviewRequired=True, ReviewStatus='Not reviewed by business',
        RequestedEvidence='Contemporaneous backup, execution log, deletion audit/transaction evidence with exact database and keys',
        ProposedAction='Retain absence and audit evidence. Do not recreate jobs/receipts, infer unused IDs, or resequence identities.'))
events=[e for r in missing_register for e in r['AuditEvents']]
assert len(events)==78 and sum(bool(r['AuditEvents']) for r in missing_register)==24
assert {e['Action'] for e in events}=={'Added'}

usage = []
for job in jobs:
    if job['ApprovedEstimateId'] is not None: continue
    jp=[p for p in payments if p['JobOrderId']==job['JobOrderId']]
    paid=sum((p['AmountPaid'] for p in jp if not p['IsVoided']),Decimal(0))
    total=job['TotalEstimatedCost']
    usage.append(dict(JobOrderId=job['JobOrderId'],Customer=job['Customer'],CustomerRecordPresent=job['CustomerId'] in cm,
        Status=job['Status'],AuthoritativeJobTotal=total,ReceiptIds=[p['PaymentId'] for p in jp],ReceiptCount=len(jp),
        NonvoidPayments=paid,OutstandingBalance=None if total is None else max(Decimal(0),total-paid),
        CustomerCredit=None if total is None else max(Decimal(0),paid-total),
        RecordedTaskIds=[t['JobTaskId'] for t in tasks if t['JobOrderId']==job['JobOrderId']],
        Qualification='Unresolved itemization; billing uses recorded job total and receipts',
        SeedJob=job['CustomerEmail'].endswith('@revcare-seed.example')))
assert len(usage)==247 and all(r['CustomerRecordPresent'] and r['RecordedTaskIds'] for r in usage)

summary=dict(EstimateCount=154,BackfilledEstimates=154,VerifiedHistoricalAgreements=0,ArithmeticConsistentOnly=123,
    DiscrepantEstimates=31,DiscrepancyTotal=sum(r['DifferenceJobMinusItemized'] for r in discrepancies),
    StoredApproved=150,StoredDraft=4,ApprovalTimestampsEqualJobStart=154,
    EstimatesWithRepeatedIdenticalLines=sum(bool(r['RepeatedIdenticalLineIds']) for r in estimate_register),
    HistoricalTaskPrices=603,SourceSupportedHistoricalTaskPrices=0,ConfirmedCatalogAssignments=0,CatalogCompatibleUnverified=603,
    MissingJobIds=30,MissingIdsWithAuditReferences=24,SurvivingAuditEvents=78,DeletionEvents=0,
    AuditEntityCounts=dict(Counter(e['EntityType'] for e in events)),UnlinkedJobsUsableFromSnapshot=len(usage),
    UnlinkedJobsWithReceipts=sum(r['ReceiptCount']>0 for r in usage),UnlinkedJobsWithoutReceipts=[r['JobOrderId'] for r in usage if r['ReceiptCount']==0],
    NonvoidReceiptTotal=sum(p['AmountPaid'] for p in payments if not p['IsVoided']),
    SnapshotScope='Saved db80735 exports; no live database read in this package; not a transactionally consistent provider backup',
    ProductionReadiness='Blocked: no verified provider backup/restore or deployment authorization')

STYLE="""body{font:15px/1.5 'Segoe UI',Arial,sans-serif;background:#f5f7fa;color:#172b3a;margin:0}header{background:#123847;color:white;padding:28px 4vw}main{padding:24px 4vw;max-width:1600px;margin:auto}a{color:#086d82}header a{color:#d8f4ff}h1{font-size:28px;margin:0 0 10px}h2{margin-top:30px}nav{display:flex;gap:18px;flex-wrap:wrap}.notice{border-left:5px solid #b06000;background:#fff4dd;padding:14px;margin:18px 0}.cards{display:flex;gap:14px;flex-wrap:wrap}.card{background:white;border:1px solid #cdd8de;padding:18px;flex:1;min-width:150px}.card strong{display:block;font-size:30px}.table-wrap{overflow:auto;background:white;border:1px solid #cdd8de}table{border-collapse:collapse;width:100%;font-size:13px}th,td{text-align:left;vertical-align:top;border-bottom:1px solid #dce3e8;padding:10px}th{background:#e6eff3;position:sticky;top:0}tr:nth-child(even){background:#f6f9fb}.num{white-space:nowrap;font-variant-numeric:tabular-nums}small{color:#52636b}details{max-width:650px}pre{white-space:pre-wrap;overflow-wrap:anywhere;font-size:12px}footer{margin:30px 0;color:#52636b}@media print{header{background:white;color:black;padding:0}nav{display:none}main{padding:0}.table-wrap{overflow:visible;border:0}th{position:static}tr{break-inside:avoid}body{background:white}.notice{border:1px solid black}}"""
NAV='<nav>'+''.join(f'<a href="{file}">{label}</a>' for file,label in [('index.html','Overview'),('estimate-register.html','154 estimates'),('discrepancies-31.html','31 discrepancies'),('task-price-register.html','603 task prices'),('missing-jobs.html','Missing IDs'),('legacy-usability.html','Historical job usability')])+'</nav>'
def page(title,body):
    return '<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta http-equiv="Content-Security-Policy" content="default-src \'none\'; style-src \'unsafe-inline\'; base-uri \'none\'; form-action \'none\'"><title>'+h(title)+' — RevCare review</title><style>'+STYLE+'</style></head><body><header><h1>'+h(title)+'</h1><p>RevCare Auto Hub · Business review · 24 November 2026 · Saved evidence only</p>'+NAV+'</header><main><div class="notice"><strong>Read-only review. No financial corrections or approvals are performed.</strong> Recorded amounts are preserved. Matching totals do not establish customer agreement. Access should be limited to authorized workshop staff; this offline report has no authentication service.</div>'+body+'<footer>Source identity and SHA-256 hashes: source-manifest.json. Exact-value registers: adjacent JSON files. Use browser Find to locate a job/estimate/task. Report files do not issue database commands or network requests.</footer></main></body></html>'
def table(headers,rows):
    return '<div class="table-wrap"><table><thead><tr>'+''.join('<th>'+h(x)+'</th>' for x in headers)+'</tr></thead><tbody>'+''.join('<tr>'+''.join('<td>'+x+'</td>' for x in row)+'</tr>' for row in rows)+'</tbody></table></div>'
def detail(value,label='Evidence and proposed review action'):
    return '<details><summary>'+h(label)+'</summary><pre>'+h(json.dumps(value,indent=2,default=serialize))+'</pre></details>'
def estimate_rows(register):
    return [[f'<strong>#{r["EstimateId"]}</strong><br>{h(r["EstimateReference"])}',f'Job #{r["JobOrderId"]}<br>{h(r["Customer"])}',money(r['AuthoritativeJobTotal']),money(r['StoredEstimateTotal']),money(r['ExistingItemizedTotal']),money(r['DifferenceJobMinusItemized']),h(r['StoredStatus'])+'<br>Approval unverified',h(r['FinancialConsistency'])+detail(r)] for r in register]

files={
 'estimate-register.json':json.dumps(estimate_register,indent=2,default=serialize),
 'discrepancies-31.json':json.dumps(discrepancies,indent=2,default=serialize),
 'task-price-register.json':json.dumps(task_register,indent=2,default=serialize),
 'missing-jobs-register.json':json.dumps(missing_register,indent=2,default=serialize),
 'legacy-usability-register.json':json.dumps(usage,indent=2,default=serialize),
 'summary.json':json.dumps(summary,indent=2,default=serialize),
 'source-manifest.json':json.dumps(dict(SourceHashes=inputs,ClassificationBasis='Observed export records and source code; no newly supplied original agreement evidence; no live refresh'),indent=2),
 'estimate-register.html':page('All 154 reconstructed estimates', '<p>123 are arithmetically consistent; 31 are discrepant. <strong>All 154 still require historical approval review.</strong> Stored Approved/Draft labels are reproduced as claims, not endorsed. All approval timestamps equal job start, including the four Draft records.</p>'+table(['Estimate','Job / customer','Authoritative job total','Stored estimate total','Existing itemized total','Job minus items','Stored status','Classification / evidence'],estimate_rows(estimate_register))),
 'discrepancies-31.html':page('31 itemization discrepancies','<p>Amounts in PHP. Difference = authoritative JobOrder.TotalEstimatedCost minus existing EstimateLine.LineTotal sum. All 31 stored estimate totals equal their source job total. No new balancing line is proposed. Expand evidence for original reference text, source association and individual line values.</p>'+table(['Estimate','Job / customer','Authoritative job total','Stored estimate total','Existing itemized total','Exact difference','Stored status','Evidence / review'],estimate_rows(discrepancies))),
 'task-price-register.html':page('603 historical task-price reviews','<p>0 source-supported historical agreements; 0 individually confirmed catalog assignments; 603 catalog-compatible values with unverified row-level origin. The known migration mechanism is evidence of how prices could have been populated, not proof of who populated each value.</p>'+table(['Task / job','Customer / service','Recorded price','Current catalog','Evidence class','Review evidence'],[[f'Task #{r["JobTaskId"]}<br>Job #{r["JobOrderId"]}',h(r['Customer'])+'<br>'+h(r['Service']),money(r['RecordedAgreedPrice']),money(r['CurrentCatalogPrice']),h(r['SourceClass']),detail(r)] for r in task_register])),
 'missing-jobs.html':page('Missing Job IDs #542–#571','<p><strong>24 IDs, 78 audit events, zero deletion events.</strong> There are six repeated groups with 13 Added events each. Test cleanup is a hypothesis consistent with source code and the pattern, not confirmed deletion evidence. No transactions may be recreated from these references.</p>'+table(['Job ID','Audit event count','First / last event (UTC)','Supported finding','Evidence / unknowns'],[[str(r['JobOrderId']),str(r['AuditEventCount']),h(r['FirstEventUtc'])+'<br>'+h(r['LastEventUtc']),h(r['EvidenceSupportedFact']),detail(r)] for r in missing_register])),
 'legacy-usability.html':page('247 unlinked historical jobs remain inspectable','<p>All 247 have customer records and recorded tasks. 235 have receipt history; Jobs #530?#541 have no recorded receipts. Empty histories are shown explicitly, not reconstructed. Billing below uses original job totals and nonvoid receipts; no itemized quotation is required. The 200 fictional seed jobs remain included and flagged until the business approves a disclosed reporting policy. These are snapshot calculations, not newly issued invoices or receipts.</p>'+table(['Job / customer','Status','Recorded total','Nonvoid paid','Outstanding','Credit','Receipts / tasks','Dataset qualification'],[[f'#{r["JobOrderId"]}<br>{h(r["Customer"])}',h(r['Status']),money(r['AuthoritativeJobTotal']),money(r['NonvoidPayments']),money(r['OutstandingBalance']),money(r['CustomerCredit']),h(r['ReceiptIds'])+'<br>'+h(r['RecordedTaskIds']),'Fictional seed — review reporting treatment' if r['SeedJob'] else 'Historical itemization unresolved'] for r in usage]))
}
cards=''.join(f'<div class="card"><strong>{value}</strong>{h(label)}</div>' for value,label in [(154,'Estimates need approval-provenance review'),(31,'Itemization discrepancies'),(603,'Unverified task prices'),(24,'Absent job IDs with audit references')])
files['index.html']=page('Historical data business-review package','<div class="cards">'+cards+'</div><h2>Decision requested</h2><p>Approve a compatibility policy that keeps original totals and receipts usable while historical itemization remains unresolved. No approval of this report authorizes cloud deployment or financial corrections.</p><h2>What is supported</h2><p>Saved job/receipt values and actual keys are documented. Backfill origin is stated in all 154 estimate Notes. Arithmetic matches for 123 estimates; this is not evidence of approval. No historical task-price agreement or estimate approval was independently verified.</p><h2>What needs review</h2><p>All 154 estimates and all 603 task prices; the 31 discrepancies deserve financial review first. The 30 absent IDs require evidence review, especially the 24 with surviving references. Previously reviewed payer/purpose questions and fictional seed treatment remain open.</p><h2>Production blockers</h2><p>No downloaded, verified MonsterASP provider backup and successful isolated restore; no deployment authorization. Local synthetic backups and BACPAC exports do not satisfy the gate. See <a href="deployment-checklist.txt">deployment-checklist.txt</a>, <a href="review-workflow.txt">review-workflow.txt</a> and <a href="BUSINESS_REVIEW_REPORT.txt">BUSINESS_REVIEW_REPORT.txt</a>.</p><h2>Read-only staff review</h2><p>Use the linked registers and browser Find. Expand evidence, compare the three totals, and collect original source documents. Record proposals in a separate controlled review register; never edit underlying financial rows through this report. No Approve, Apply, Save, Reprice or Reconcile controls exist.</p>')

for template in (Path(__file__).parent/'templates').glob('*.txt'):
    files[template.name]=template.read_text(encoding='utf-8')
discrepancy_text=['31 ESTIMATE ITEMIZATION DISCREPANCIES — READ ONLY', 'Amounts PHP. Difference = authoritative job total minus existing itemized total.', 'No amount is reconstructed, balanced or classified as a missing charge.', '']
for r in discrepancies:
    discrepancy_text += [f"Estimate #{r['EstimateId']} / Job #{r['JobOrderId']} / {r['Customer']}",f"Authoritative job total: {r['AuthoritativeJobTotal']}; stored estimate total: {r['StoredEstimateTotal']}; existing itemized total: {r['ExistingItemizedTotal']}; exact difference: {r['DifferenceJobMinusItemized']}",f"Historical reference text: {r['HistoricalReferenceText'] or 'Unknown / absent'}",f"Evidence: {r['BackfillNote']}",f"Stored status: {r['StoredStatus']}; approval provenance: {r['ApprovalProvenance']}",f"Source association: {r['JobAssociation']}; line IDs: {[l['EstimateLineId'] for l in r['ExistingLines']]}",'Human review required; preserve job total and receipts.','']
files['discrepancies-31.txt']='\n'.join(discrepancy_text)
code_sources=['CRM.infrastructure/services/HistoricalCompatibility.cs','CRM.infrastructure/services/Legacy3NfReconciliationService.cs','CRM.infrastructure/data/WorkshopRules.cs','CRM_Revcare/CrmTransactionsForm.Operations.cs','CRM_Revcare/CrmTransactionsForm.cs','CRM_Revcare/ReportTemplateBuilder.cs','CRM.domain/entities/Payment.cs','tests/Verification.Smoke/Program.cs','tools/Workshop.Database/ProductionCompatibilityTests.cs','artifacts/historical-safety/Legacy3NfReconciliationService.before.txt']
files['code-evidence-hashes.json']=json.dumps({s:hashlib.sha256((ROOT/s).read_bytes()).hexdigest() for s in code_sources},indent=2)

parser=argparse.ArgumentParser()
parser.add_argument('--output',default='artifacts/business-review-20261124')
args=parser.parse_args()
out=(ROOT/args.output).resolve()
if ROOT not in out.parents: raise ValueError('Output must be inside workspace')
out.mkdir(parents=True,exist_ok=False)
for name,content in files.items():
    with (out/name).open('x',encoding='utf-8') as f: f.write(content)
print(json.dumps(summary,indent=2,default=serialize))
print('Created new read-only package:',out)

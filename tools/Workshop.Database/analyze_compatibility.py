import json
from pathlib import Path
from decimal import Decimal
p=Path('artifacts/historical-compatibility')
def read(n): return json.loads((p/(n+'.json')).read_text(),parse_float=Decimal)
def write(n,v): (p/(n+'.json')).write_text(json.dumps(v,indent=2,default=str))
inventory={'Jobs':[],'Estimates':[]}
for mode in ('local','cloud'):
    jobs=read(mode+'-keys'); estimates=read(mode+'-estimates')
    inventory['Jobs'] += [{'Id':j['JobOrderId'],'CreatedAt':j['CreatedAt']} for j in jobs]
    inventory['Estimates'] += [{'Id':e['EstimateId'],'CreatedAt':e['CreatedAt']} for e in estimates]
jobs=read('cloud-keys'); estimates=read('cloud-estimates'); lines=read('cloud-estimate-lines'); audits=read('cloud-audit')
review=[]
for e in estimates:
    links=[j['JobOrderId'] for j in jobs if j['ApprovedEstimateId']==e['EstimateId']]
    source=next((j for j in jobs if e['EstimateNumber']==f"EST-{j['JobOrderId']:06}"),None)
    total=sum((l['LineTotal'] for l in lines if l['EstimateId']==e['EstimateId']),Decimal(0))
    review.append(dict(EstimateId=e['EstimateId'],Reference=e['EstimateNumber'],StoredStatus=e['Status'],StoredApprovedAt=e['ApprovedAt'],Notes=e['Notes'],LinkedJobs=links,GeneratedReferenceJob=source['JobOrderId'] if source else None,StoredTotal=e['TotalAmount'],LineSum=total,Difference=e['TotalAmount']-total,ApprovalTimeEqualsJobStart=bool(source and e['ApprovedAt']==source['StartedAt']),Provenance='Reconstructed/backfilled; historical customer approval UNVERIFIED',QualificationRequired=True,Action='Read-only qualification now; preserve original status/time as imported assertions. Any later database correction requires evidence, audit and approval.'))
assert len(review)==154
write('estimate-review-154',review)
gaps=[]
for jid in range(542,572):
    events=[]
    for a in audits:
        try: changes=json.loads(a['Changes'])
        except ValueError: continue
        for field in ('JobOrderId','ConvertedJobOrderId','LinkedJobOrderId'):
            value=changes.get(field,{})
            if isinstance(value,dict) and jid in value.values():
                events.append(dict(AuditEntryId=a['AuditEntryId'],At=a['OccurredAtUtc'],Entity=a['EntityType'],Action=a['Action'],Actor=a['Actor'],Changes=changes));break
    gaps.append(dict(JobOrderId=jid,PresentNow=any(j['JobOrderId']==jid for j in jobs),References=events,Conclusion='Previously referenced in committed audit; currently absent' if events else 'Absent; no direct positive-key audit reference found',DeletionCause='Unknown: no verified deletion transaction/source backup; never recreate from sequence position'))
write('missing-542-571-investigation',gaps)
local_seed={j['CustomerEmail']:j for j in read('local-keys') if (j.get('CustomerEmail') or '').endswith('@revcare-seed.example')}
key_map=[]
for job in jobs:
    if job.get('CustomerEmail') in local_seed:
        local=local_seed[job['CustomerEmail']]
        key_map.append(dict(SeedCustomer=job['CustomerEmail'],CloudJobId=job['JobOrderId'],LocalJobId=local['JobOrderId'],CloudAmount=job['TotalEstimatedCost'],LocalAmount=local['TotalEstimatedCost'],AmountsMatch=job['TotalEstimatedCost']==local['TotalEstimatedCost']))
assert len(key_map)==200 and all(m['AmountsMatch'] for m in key_map)
write('seed-key-map-local-cloud',key_map)
text=['MISSING ID REVIEW: CURRENT CLOUD db80735 (SELECT-only)', 'Historical audit references do not reconstruct a deleted job or prove the deletion cause.', '']
for row in gaps:
    ids=', '.join(str(e['AuditEntryId']) for e in row['References']) or 'none'
    text.append(f"Job #{row['JobOrderId']}: ABSENT. Direct positive-key audit entries: {ids}. {row['Conclusion']}. Deletion cause unknown.")
(p/'missing-542-571-investigation.txt').write_text('\n'.join(text))
text=['154 EXISTING CLOUD ESTIMATES: REVIEW ONLY; NO CORRECTIONS EXECUTED', 'All are reconstructed/backfilled, with customer approval unverified.', '']
for row in review:
    text.append(f"Estimate #{row['EstimateId']} ({row['Reference']}): status {row['StoredStatus']}; linked jobs {row['LinkedJobs']}; stored total {row['StoredTotal']}; line sum {row['LineSum']}; difference {row['Difference']}; ApprovedAt equals source job StartedAt: {row['ApprovalTimeEqualsJobStart']}. Qualification required; preserve original assertions and route any correction for approval.")
(p/'estimate-review-154.txt').write_text('\n'.join(text))
Path('CRM.infrastructure/data/HistoricalCompatibilityInventory.json').write_text(json.dumps(inventory,separators=(',',':')))
print('Inventory:',len(inventory['Jobs']),'job fingerprints;',len(inventory['Estimates']),'estimate fingerprints')
print('Estimate review:',len(review),'backfilled;',sum(r['ApprovalTimeEqualsJobStart'] for r in review),'timestamps equal start;',sum(r['Difference']!=0 for r in review),'line/total discrepancies')
print('Absent IDs with positive-key audit references:',sum(bool(r['References']) for r in gaps),'of 30')

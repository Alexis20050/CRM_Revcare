"""Independent, read-only completeness/arithmetic/source/link checks for a package."""
import hashlib
import json
import sys
from decimal import Decimal
from html.parser import HTMLParser
from pathlib import Path

root=Path(__file__).resolve().parents[2]
p=root/(sys.argv[1] if len(sys.argv)>1 else 'artifacts/business-review-20261124')
def read(name):return json.loads((p/name).read_text(encoding='utf-8-sig'))
est=read('estimate-register.json');disc=read('discrepancies-31.json');tasks=read('task-price-register.json');gaps=read('missing-jobs-register.json');usage=read('legacy-usability-register.json')
assert len(est)==154 and len({r['EstimateId'] for r in est})==154
assert len(disc)==31 and {r['EstimateId'] for r in disc}==set(range(1,31))|{32}
assert len(tasks)==603 and len({r['JobTaskId'] for r in tasks})==603
assert len(gaps)==30 and {r['JobOrderId'] for r in gaps}==set(range(542,572))
assert sum(r['AuditEventCount'] for r in gaps)==78 and sum(r['AuditEventCount']>0 for r in gaps)==24
assert all(not r['ConfirmedDeletion'] for r in gaps)
for r in est:
    total=sum((Decimal(l['LineTotal']) for l in r['ExistingLines']),Decimal(0))
    assert total==Decimal(r['ExistingItemizedTotal'])
    assert Decimal(r['AuthoritativeJobTotal'])-total==Decimal(r['DifferenceJobMinusItemized'])
    assert r['HistoricalAgreementVerified'] is False
assert sum(Decimal(r['DifferenceJobMinusItemized']) for r in disc)==Decimal('152000.00')
assert all(not t['SourceSupportedHistoricalAgreement'] and not t['ConfirmedCatalogAssignment'] for t in tasks)
assert len(usage)==247 and sum(r['ReceiptCount']==0 for r in usage)==12
runtime=read('usability-verification.json')
assert runtime['Verification']=='PASS' and runtime['Jobs']==247 and runtime['ReceiptsRendered']==235 and runtime['EmptyHistories']==12
actual={r['JobOrderId']:r for r in runtime['Results']}
for r in usage:
    a=actual[r['JobOrderId']]
    for field,other in [('AuthoritativeJobTotal','PreservedTotal'),('NonvoidPayments','NonvoidPayments'),('OutstandingBalance','OutstandingBalance'),('CustomerCredit','CustomerCredit')]:
        assert Decimal(str(a[other]))==Decimal(r[field])
for file,digest in read('source-manifest.json')['SourceHashes'].items():
    assert hashlib.sha256((root/file).read_bytes()).hexdigest()==digest, file
for file,digest in read('code-evidence-hashes.json').items():
    assert hashlib.sha256((root/file).read_bytes()).hexdigest()==digest, file
class Inspect(HTMLParser):
    def __init__(self):super().__init__();self.rows=0
    def handle_starttag(self,tag,attrs):
        assert tag not in ('script','form','input','button','iframe'),tag
        if tag=='tr':self.rows+=1
        for key,value in attrs:
            assert not key.lower().startswith('on'),key
            if key in ('href','src'):
                assert '://' not in value and not value.startswith(('javascript:','data:'))
                assert (p/value.split('#')[0]).exists(),value
expected={'estimate-register.html':155,'discrepancies-31.html':32,'task-price-register.html':604,'missing-jobs.html':31,'legacy-usability.html':248}
for file in p.glob('*.html'):
    check=Inspect();check.feed(file.read_text(encoding='utf-8'))
    if file.name in expected:assert check.rows==expected[file.name],(file.name,check.rows)
print('PASS: 154 estimates / 31 discrepancies / 603 task prices / 30 absent IDs / 78 audit events / 247 usable jobs.')
print('PASS: Independent Decimal arithmetic agrees with actual application calculations for every job.')
print('PASS: Evidence and inspected application-source hashes unchanged; all report links resolve; no scripts/forms/write controls.')

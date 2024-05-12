import http.client
import json

conn = http.client.HTTPSConnection("localhost", 8080)
payload = json.dumps({
  "CurrentPos": [
    34.80500045,
    -103.56373007
  ],
  "LastPos": [
    34.80429903,
    -103.56431205
  ],
  "LastTimestamp": 1678096374,
  "Topics": [],
  "Filter": "",
  "Type": 0
})
headers = {
  'Authorization': 'Bearer test1XXXXXXXXXXX',
  'Content-Type': 'application/json'
}
conn.request("POST", "/nearme", payload, headers)
res = conn.getresponse()
data = res.read()
print(data.decode("utf-8"))
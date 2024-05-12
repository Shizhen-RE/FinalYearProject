import http.client
import json
import random
import string
import time
import datetime

def message_producer(userToken):
  random.seed()
  conn = http.client.HTTPConnection("localhost", 8080)
  while True:
    content = "".join(random.choice(string.ascii_letters) for i in range(random.randint(5, 50)))
    now = time.time()
    payload = json.dumps({
      "Id": "",
      "User": userToken,
      "Topic": "640647a6ed81f326645108d4",
      "Coordinates": [
        43.4723 + (random.random() - 0.5)/100,
        -80.5449 + (random.random() - 0.5)/100,
        337 + random.random() * 3
      ],
      "Timestamp": int(now),
      "Likes": 0,
      "IsLiked": False,
      "Content": content,
      "IsAR": False,
      "ImageFormat": "text",
      "Preview": "",
      "SizeMultiplier": 0
    })
    headers = {
      'Authorization': 'Bearer ' + userToken,
      'Content-Type': 'application/json'
    }
    conn.request("POST", "/message", payload, headers)
    res = conn.getresponse().read()
    print(datetime.datetime.now().strftime("%H:%M:%S.%f") + ": posted request with content " + content)
    time.sleep(2)
  
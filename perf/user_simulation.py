import http.client
import json
import random
import time
import datetime

def user_simulation(userToken):
    random.seed()
    userNewPos = [43.4723, -80.5449]
    conn = http.client.HTTPConnection("localhost", 8080)
    while True:
        now = time.time()
        userPos = [0.0, 0.0]
        userNewPos = [userNewPos[0] + (random.random() - 0.5)/5000, userNewPos[1] + (random.random() - 0.5)/5000]
        payload = json.dumps({
            "CurrentPos": userNewPos,
            "LastPos": userPos,
            "LastTimestamp": int(now),
            "Topics": ["640647a6ed81f326645108d4"],
            "Filter": "",
            "Type": 0
        })
        headers = {
            'Authorization': 'Bearer ' + userToken,
            'Content-Type': 'application/json'
        }
        conn.request("POST", "/nearme", payload, headers)
        res = conn.getresponse()
        resJson = json.loads(res.read().decode("utf-8"))
        if (type(resJson["Messages"]) == list):
            for msg in resJson["Messages"]:
                print(datetime.datetime.now().strftime("%H:%M:%S.%f") + ": " + msg["Id"] + ", " + msg["Content"])
        time.sleep(0.5)

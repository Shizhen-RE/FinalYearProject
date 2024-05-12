from multiprocessing import Process
from message_producer import message_producer
from user_simulation import user_simulation
from time import sleep
from random import random
from os import kill
from signal import SIGINT

pCon = Process(target=user_simulation, args=("test2XXXXXXXXXXX",))
pPro = Process(target=message_producer, args=("test1XXXXXXXXXXX",))

print("start consumer")
pCon.start()
sleep(2 + random())
print("start producer")
pPro.start()

sleep(20)

print("killing processes")
kill(pCon.pid, SIGINT)
kill(pPro.pid, SIGINT)

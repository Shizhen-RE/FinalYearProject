import argparse

from pymongo import MongoClient

DB_URI = "mongodb+srv://aidanfo:50VsPsP5MSUJG8AG@near-me.c9eaw.mongodb.net/?retryWrites=true&w=majority&authSource=admin"
DB_NAME = "test"
COL_NAME = "Geomesh"

class Geomesh:
    def __init__(self, lat, lon, length):
        self.Lat = lat
        self.Lon = lon
        self.length = length

    def get(self):
        return {
            "Latitude": float(self.Lat),
            "Longitude": float(self.Lon),
            "Length": float(self.length)
        }
    
    def toString(self):
        return f"Latitude: {self.Lat}, Longitude: {self.Lon}, Length: {self.length}"

def main():
    # connect to the database
    client = MongoClient(DB_URI)
    db = client[DB_NAME]
    col = db[COL_NAME]

    # if db is not empty, clean up
    if col.count_documents({}) != 0:
        col.delete_many({})
    
    # create some geomesh for the Canada region
    minLat = 40
    maxLat = 80
    minLon = -140
    maxLon = -60
    initLength = 10 # 10 deg ~= 1110 km
    geomesh_list = []

    for i in range(4):
        for j in range(8):
            geomesh_list.append(Geomesh(lat=minLat+(i*initLength), lon=minLon+(j*initLength), length=initLength).get())
    
    col.insert_many(geomesh_list)


if __name__ == "__main__":
    # get command line arguments
    parser = argparse.ArgumentParser()
    parser.add_argument('--uri', dest='db_uri', help='MongoDB URI', type=str)
    parser.add_argument('--db', dest='db_name', help='MongoDB database name', type=str)
    args = parser.parse_args()

    if args.db_uri is not None:
        DB_URI = args.db_uri
    if args.db_name is not None:
        DB_NAME = args.db_name

    main()

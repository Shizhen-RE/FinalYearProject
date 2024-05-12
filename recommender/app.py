from flask import Flask
from flask import request, jsonify
import logging
 
from recommender import worker as worker
from recommender import config as config

# create the app
app = Flask("recommender")

# configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s %(levelname)s (%(name)s): %(message)s', datefmt='%Y-%m-%d %H:%M:%S')
logger = logging.getLogger(__name__)

@app.route("/")
def home():
    return "NeAR Me Topic Recommender"

@app.route("/getRecommendations/", methods=['GET'])
def recommend():
    userId = request.args.get('userId')
    regionId = request.args.get('regionId')

    recommended_topic_ids = worker.get_recommendations(userId, regionId)

    # return f"Recommending topics for user {userId} in region {regionId}."
    return jsonify({'recommendations': recommended_topic_ids})

@app.before_first_request
def start():
    worker.start()

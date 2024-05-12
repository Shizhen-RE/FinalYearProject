# Constants
MSG_COUNT_MULT = 1.1        # 10% increase = threshold to recompute topic similarities
USER_COUNT_MULT = 1.1       # 10% increase = threshold to recompute user similarities

## Database
DB_URI = "mongodb+srv://aidanfo:50VsPsP5MSUJG8AG@near-me.c9eaw.mongodb.net/?retryWrites=true&w=majority&authSource=admin"
# DB_URI = "mongodb://127.0.0.1:27017" # local testing
# DB_URI = "mongodb:27017" # docker-compose

MAIN_DB = "Main"
RECOMMENDER_DB = "recommender"
ENCODER_COLLECTION = "encodings"
UB_CACHE_COLLECTION = "ub_rec_cache"    # user-based recommender cache
TB_CACHE_COLLECTION = "tb_rec_cache"    # topic-based recommender cache
META_COLLECTION = "meta"                # meta data cache
MSG_COUNT_TYPE = "MESSAGE_COUNT"        # type in meta cache
USER_COUNT_TYPE = "USER_COUNT"          # type in meta cache

## Topic-based recommender
MSG_SAMPLE_SIZE = 100
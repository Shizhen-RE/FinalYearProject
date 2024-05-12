#!/bin/bash

SCRIPT="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"
DIR="$(dirname "$SCRIPT")"

host=localhost
port=8080
path="$1"
method=
content_type=
message_body=
form_fields=
query=

if [[ "$1" == disableAuth ]] && [[ $# == 2 ]]; then
    cd "$DIR"/../NearMe/nearme_backend/src/main/java/com/nearme/nearme_backend/controller/
    for file in `ls`; do
        sed "s/.*String userId =.*/String userId = \"$2\";/g" -i $file
    done
    exit 0
elif [[ "$1" == addLocation ]] && [[ $# == 5 ]]; then
    method=post
    content_type=form
    touch "$1"
    form_fields=\
"
-F SupportingDoc=@$1
-F latitude=$3 \
-F longitude=$4 \
-F altitude=$5
"
elif [[ "$1" == addMessage ]] && [[ $# == 10 ]]; then
    method=post
    content_type=json
    message_body=\
"
{
    \"Topic\": \"$2\",
    \"Longitude\": $3,
    \"Latitude\": $4,
    \"Altitude\": $5,
    \"Timestamp\": $6,
    \"Content\": \"$7\",
    \"IsAR\": $8,
    \"Width\": $9,
    \"Height\": ${10}
}
"
elif [[ "$1" == deleteLocation ]] && [[ $# == 2 ]]; then
    method=post
    query="location_id=$2"
elif [[ "$1" == deleteMessage ]] && [[ $# == 2 ]]; then
    method=post
    query="message_id=$2"
elif ([[ "$1" == getComments ]] || [[ "$1" == getContents ]]) && [[ $# -ge 8 ]]; then
    method=post
    content_type=json
    message_body=\
"
{
    \"CurrentLatitude\": $2,
    \"CurrentLongitude\": $4,
    \"OldLatitude\": $4,
    \"OldLongitude\": $5,
    \"LastTimestamp\": $6,
    \"Filter\": $7,
    \"Topics\": ["
    shift 7
    for topic in $@; do
        message_body="$message_body \"$topic\","
    done
    message_body="${message_body%,} ]
}
"
elif [[ "$1" == getPublications ]] && [[ $# == 2 ]]; then
    method=get
    query="start-time=$2"
elif ([[ "$1" == getSubscriptions ]] || [[ "$1" == getLocations ]]) && [[ $# == 1 ]]; then
    method=get
elif [[ "$1" == getTopicList ]] && [[ $# == 3 ]]; then
    method=get
    query="latitude=$2&longitude=$3"
elif [[ "$1" == healthCheck ]] && [[ $# == 1 ]]; then
    method=get
elif [[ "$1" == isVerified ]] && [[ $# == 4 ]]; then
    method=get
    query="latitude=$2&longitude=$3&altitude=$4"
elif [[ "$1" == updateLocation ]] && [[ $# == 3 ]]; then
    method=post
    query="location_id=$2&new_name=$3"
elif ([[ "$1" == subscribeTopics ]] || [[ "$1" == unsubscribeTopics ]]) && [[ $# -ge 2 ]]; then
    method=post
    content_type=json
    message_body="["
    shift 1
    for topic in $@; do
        message_body="$message_body \"$topic\","
    done
    message_body="${message_body%,} ]"
else
    echo 'Usage: apireq.sh request arguments...'
    echo
    echo 'Non-API Requests:'
    echo '    disableAuth user_uid'
    echo
    echo 'Requests:'
    echo '    addLocation supporting_doc latitude longitude altitude'
    echo '    addMessage topic long lat alt timestamp content is_ar width height'
    echo '    deleteLocation location_id'
    echo '    deleteMessage message_id'
    echo '    getComments new_long new_lat old_long old_lat timestamp filter topics...'
    echo '    getContents new_long new_lat old_long old_lat timestamp filter topics...'
    echo '    getLocations'
    echo '    getPublications start_time'
    echo '    getSubscriptions'
    echo '    getTopicList lat long'
    echo '    healthCheck'
    echo '    isVerified latitude longitude altitude'
    echo '    subscribeTopics topics...'
    echo '    updateLocation location_id new_name'
    echo '    unsubscribeTopics topics...'
    exit 1
fi

if [[ "$method" == post ]]; then
    if [[ $content_type == json ]]; then
        echo \
"`
curl \
    --header 'Content-Type: application/json' \
    --header 'Authorization: Bearer 1234' \
    --request POST \
    --data "$message_body" \
    "$host:$port/$path?$query" 2>/dev/null
`"
    elif [[ $content_type == form ]]; then
        echo \
"`
curl \
    --header 'Authorization: Bearer 1234' \
    $form_fields \
    "$host:$port/$path?$query" 2>/dev/null
`"
    else
        echo \
"`
curl \
    --header 'Authorization: Bearer 1234' \
    --request POST \
    "$host:$port/$path?$query" 2>/dev/null
`"
    fi
elif [[ "$method" == get ]]; then
    echo \
"`
curl \
    --header 'Authorization: Bearer 1234' \
    "$host:$port/$path?$query" 2>/dev/null
`"
else
    exit 1
fi


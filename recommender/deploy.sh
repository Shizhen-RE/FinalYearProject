#!/bin/bash

# This script attempts to build and deploy the recommender app image

# Pre-requisites:
# - install kubectl (Kubernetes CLI)
# - install gcloud (Google cloud CLI)
# - access to the google cloud project nearme-356517

set -ex

# Google cloud environment variables
GCP_project_ID="nearme-356517"
GCP_repo="nearme-docker-repo"
GCP_repo_location="northamerica-northeast2"
GKE_cluster="nearme-server-cluster"
GKE_cluster_location="us-east1"

# build variables
image_name="recommender"

# deployment variables
deployment_config="./deployment.yml"
deployment_name="recommender"

if [[ $(gcloud config list account --format "value(core.account)") ]]; then
    echo "Already logged in to gcloud."
else
    gcloud auth activate-service-account --key-file nearme-356517-1a3b10b60f62.json
    echo "Logged in to gcloud."
fi

gcloud container clusters get-credentials $GKE_cluster --region=$GKE_cluster_location --project=$GCP_project_ID
if [ $? -eq 0 ]; then
    echo "Successfully attached to cloud resources."
else
    echo "Failed to attach to cloud resources."
    exit
fi


echo "Building recommender image..."
# # Option 1: use Gooogle Cloud Build to build the image directly on cloud and store in Google cloud artifact registry
gcloud builds submit --region=global --tag "$GCP_repo_location-docker.pkg.dev/$GCP_project_ID/$GCP_repo/$image_name:latest"

echo "Deploying recommender to GKE cluster..."
kubectl apply -f $deployment_config

echo "Successfully deployed recommender to GKE."

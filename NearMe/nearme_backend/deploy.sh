#!/bin/bash

# This script attempts to build and deploy the server image to GKE.

# Pre-requisites:
# - install maven
# - install Docker
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
GKE_node_port=30876

# build variables
image_name="nearme_backend"

# deployment variables
deployment_config="./deployment.yml"
deployment_name="nearme-server"

echo "Creating jar file..."
mvn package -B -DskipTests

# Google cloud build ignores files/folders listed in .gitignore or .dockerignore
# so we need to bring out the .jar file from the target/ folder before building the image
cp target/nearme_backend-0.0.1-SNAPSHOT.jar nearme_backend.jar

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

echo "Building server image..."
# Option 1: use Gooogle Cloud Build to build the image directly on cloud and store in Google cloud artifact registry
gcloud builds submit --region=global --tag "$GCP_repo_location-docker.pkg.dev/$GCP_project_ID/$GCP_repo/$image_name:latest"

# Option 2: use docker to build image locally and then push the image to Google cloud artifact registry
# Drawback: the resulting image is affected by the machine's architecture
## docker build image
# docker build -t "$GCP_repo_location-docker.pkg.dev/$GCP_project_ID/$GCP_repo/$image_name:latest" .
# gcloud auth configure-docker ${GCP_repo_location}-docker.pkg.dev
## docker login
# cat nearme-356517-1a3b10b60f62.json | docker login -u _json_key --password-stdin https://northamerica-northeast2-docker.pkg.dev
## push image to google cloud
# docker push "$GCP_repo_location-docker.pkg.dev/$GCP_project_ID/$GCP_repo/$image_name"

# cleanup directory after image build
rm nearme_backend.jar

echo "Deploying server to GKE cluster..."
kubectl apply -f $deployment_config

echo "Successfully deployed server to GKE."

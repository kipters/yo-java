#!/usr/bin/env sh

awslocal dynamodb create-table --cli-input-json file:///opt/init-data/users.json

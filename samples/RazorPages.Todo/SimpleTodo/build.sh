#! /bin/bash

dotnet publish -o $(PWD)/output
docker build -t simpletodo ./output
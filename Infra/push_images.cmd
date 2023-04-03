az acr login -n cronboarding

#idp
docker tag externalscreeningidp:latest cronboarding.azurecr.io/externalscreeningidp:latest
docker tag externalscreeningidp:latest cronboarding.azurecr.io/externalscreeningidp:1.0
docker push cronboarding.azurecr.io/externalscreeningidp:latest

#external api
docker tag externalscreeningapi:latest cronboarding.azurecr.io/externalscreeningapi:latest
docker tag externalscreeningapi:latest cronboarding.azurecr.io/externalscreeningapi:1.0
docker push cronboarding.azurecr.io/externalscreeningapi:latest

#onboarding api
docker tag onboardingserver:latest cronboarding.azurecr.io/onboardingserver:latest
docker tag onboardingserver:latest cronboarding.azurecr.io/onboardingserver:1.0
docker push cronboarding.azurecr.io/onboardingserver:latest




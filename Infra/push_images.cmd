az acr login -n cronboarding
$version = "0.11"

#idp
docker build -f "D:\Projects\gh\Onboarding\Remote\ExternalScreening.Idp\Dockerfile" --force-rm -t externalscreeningidp  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=ExternalScreening.Idp" "D:\Projects\gh\Onboarding"

docker tag externalscreeningidp:latest cronboarding.azurecr.io/externalscreeningidp:latest
docker tag externalscreeningidp:latest cronboarding.azurecr.io/externalscreeningidp:$version
docker push cronboarding.azurecr.io/externalscreeningidp:latest
docker push cronboarding.azurecr.io/externalscreeningidp:$version

#external api
docker build -f "D:\Projects\gh\Onboarding\Remote\ExternalScreening.Api\Dockerfile" --force-rm -t externalscreeningapi  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=ExternalScreening.Api" "D:\Projects\gh\Onboarding"

docker tag externalscreeningapi:latest cronboarding.azurecr.io/externalscreeningapi:latest
docker tag externalscreeningapi:latest cronboarding.azurecr.io/externalscreeningapi:$version
docker push cronboarding.azurecr.io/externalscreeningapi:latest
docker push cronboarding.azurecr.io/externalscreeningapi:$version

#onboarding app
docker build -f "D:\Projects\gh\Onboarding\Server\Dockerfile" --force-rm -t onboardingserver  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=Onboarding.Server" "D:\Projects\gh\Onboarding"

docker tag onboardingserver:latest cronboarding.azurecr.io/onboardingserver:latest
docker tag onboardingserver:latest cronboarding.azurecr.io/onboardingserver:$version
docker push cronboarding.azurecr.io/onboardingserver:latest
docker push cronboarding.azurecr.io/onboardingserver:$version




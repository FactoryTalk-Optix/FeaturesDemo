# FeaturesDemo
FactoryTalk Optix FeaturesDemo. This contains most of the features exposed by FT Optix

## Informations
This demo was originally designed in Uniqo, it has been recreated in Optix.

## Cloning the repository
There are multiple ways to download this project, here are few of them

### Clone repository
1. Click on the green `CODE` button in the top right corner
2. Select `HTTPS` and copy the provided URL
3. Open FT Optix IDE
4. Click on `Open` and select the `Remote` tab
5. Paste the URL from step 2
6. Click `Open` button in bottom right corner to start cloning process

## Preparing the demo
This demo can run without any external tools, only few pages requires extra hardware/software components

### Machine to Machine demo
Machine to Machine client (repo called `FeaturesDemo_M2M_Client`) can be found in this organization, this client application fetches some pages from the main Demo and exposes a PanelLoader that can be toggled via two buttons.

Both projects need to be configured with proper IP and port in the OPC/UA configuration if used on different machines

### OPC/UA Server
This demo acts as an OPC/UA server at port `59100`, an external client should be used such as [UaExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html)

### Multiple PLCs
PLC code can be found `DemoUniqoPlc` and needs to be downloaded on the two controllers. This is achived by replicating the same TAGs structure on both PLC and by indexing the proper device via aliases

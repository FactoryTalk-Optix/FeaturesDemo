# FeaturesDemo
FactoryTalk Optix FeaturesDemo. This contains most of the features exposed by FT Optix

## Deprecation notice

This demo is now archived and won't be maintained anymore, please use the FeaturesDemo2 instead

## Informations
This demo was originally designed in Uniqo, it has been recreated in Optix.

## Cloning the repository
There are multiple ways to download this project, here are a few of them

### Clone repository
1. Click on the green `CODE` button in the top right corner
2. Select `HTTPS` and copy the provided URL
3. Open FT Optix IDE
4. Click on `Open` and select the `Remote` tab
5. Paste the URL from step 2
6. Click `Open` button in the bottom right corner to start the cloning process

## Preparing the demo
This demo can run without any external tools, only few pages require extra hardware/software components

### Machine to Machine demo
Machine to Machine client (repo called `FeaturesDemo_M2M_Client`) can be found in this organization, this client application fetches some pages from the main Demo and exposes a PanelLoader that can be toggled via two buttons.

Both projects need to be configured with proper IP and port in the OPC/UA configuration if used on different machines

### OPC/UA Server
This demo acts as an OPC/UA server at port `59100`, an external client should be used such as [UaExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html)

### Multiple PLCs
PLC code can be found `ProjectFiles/DemoUniqoPlc` and needs to be downloaded on the two controllers. This is achieved by replicating the same TAG structure on both PLCs and by indexing the proper device via aliases

### Disclaimer

Rockwell Automation maintains these repositories as a convenience to you and other users. Although Rockwell Automation reserves the right at any time and for any reason to refuse access to edit or remove content from this Repository, you acknowledge and agree to accept sole responsibility and liability for any Repository content posted, transmitted, downloaded, or used by you. Rockwell Automation has no obligation to monitor or update Repository content

The examples provided are to be used as a reference for building your own application and should not be used in production as-is. It is recommended to adapt the example for the purpose, observing the highest safety standards.

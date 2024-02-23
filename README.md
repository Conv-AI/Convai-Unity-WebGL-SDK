# Convai Unity WebGL SDK

Welcome to the Convai Unity WebGL SDK repository. Convai is a powerful tool that enables AI characters in games and virtual worlds to have human-like conversation capabilities and more. With Convai, developers can add a backstory, knowledge base, voice, and overall intelligence to their character assets to converse naturally with players and carry out actions.

## Description

Convai includes the complete conversation pipeline, which encompasses Speech Recognition, Language Understanding and Generation, Text-to-Speech, Text-to-Action, and Lipsync. Our focus is on creating characters that are as life-like as possible, accessible to any developer to direct and integrate into their projects.

Check out our [Documentation](https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin) to get started!

## Resources

- [Documentation](https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin): Guide through the plugin's features and capabilities.
- [Quick Start Video](https://youtu.be/Vhr7IvfITgU): Get started quickly and easily.
- [WebGL Turorial Video](https://youtu.be/JXjcHEnEPCQ)
- [Troubleshooting Guide](https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/troubleshooting-guide): Solve commonly faced issues.
- [Tutorial Series](https://www.youtube.com/playlist?list=PLn_7tCx0ChipYHtbe8yzdV5kMbozN2EeB): Master the plugin's full potential.
- [Convai Website](https://convai.com/): Create custom characters and try them out.
- [Discord Server](https://discord.gg/5mRtu2WhEm): Get help with any issues while using our plugin.

For any questions, reach out to support@convai.com.

## Highlights

- **Intelligent Conversations**: Real-time open-ended conversation capabilities for natural and dynamic interactions.
- **Actions**: Characters understand the environment and perform actions based on conversations, verbal commands, and events.
- **Knowledge Base**: Add unlimited knowledge to your characters, making them experts or grounding them in lore and backstories.
- **Intelligent Animations**: Integrated with Lip-Sync and custom animations with conversation flow.
- **Avatar Integrations**: Out-of-the-box integration with popular avatar platforms like Reallusion Character Creator 4 and Ready Player Me.
- **Text or Talk**: Interact with characters using text or voice input.
- **Production Ready**: Deployable to hundreds of users under our commercial agreement.

## Why Convai?

- Engage in open-ended conversations with responsive and intelligent characters.
- Quickly give your character an interesting background using the Convai Website.
- Easily bring characters into your projects with the Convai Unity Plugin.
- Well-documented, easy-to-read, and highly customizable code to accommodate any game logic.

## Technical Details

- **Documentation**: [Click here](https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/building-for-webgl) to go to our documentation for WebGL.

## Steps for Setting Up in Unity Versions Before 2022.3:

1. Click on `File` > `Project Settings`.
2. Navigate to `Player` > `Other Settings`.
3. Uncheck the `Assemble Version Validation` option.

## Prerequisites

Before you begin, ensure you have the following prerequisites installed on your system:

- Unity (Version compatible with Convai SDK)
- Git

## Installation

1. **Download the Convai Unity WebGL SDK**: Clone this repository or download the SDK as a `.zip` file and extract it to your local machine.
2. **Create a New Unity Project**: Open Unity and create a new project, or use an existing Unity project where you want to integrate Convai.
3. **Import the Convai Plugin**: In Unity, go to `Assets` > `Import Package` > `Custom Package`. Navigate to the extracted SDK folder and select the Convai plugin package to import it into your project.

## Configuration

1. **Resolve Dependencies**: After importing the Convai plugin, you may encounter errors related to "Ready Player Me" dependencies. To resolve these, edit the `manifest.json` file in your Unity project's Packages folder to include the necessary packages. The required package details are listed in the Convai documentation.
2. **Set Up the Convai Plugin**: In Unity, click on the `Convai` menu and open the `Convai Setup` window. Enter your API key, which can be obtained from the Convai website, and click `Start`.

## Building for WebGL

1. **Switch to WebGL Platform**: Go to `File` > `Build Settings`, select the WebGL platform, and click on `Switch Platform`.
2. **Configure WebGL Template**: Move the WebGL template folder from the Convai folder into the Unity `Assets` folder. In the `Build Settings` window, click on `Player Settings` and ensure the `Convai PWA` template is selected.
3. **Build and Run**: Choose a folder for the build output and click `Build`. The first build may take longer, but subsequent builds will be faster. Once the build is complete, a local host page will open in your default browser for testing.

## Interacting with the Build
To interact with the character in your WebGL build, move closer to the character and press the 'T' key to initiate a conversation.
To open settings menu press '0'

## Support
If you encounter any issues or have questions, please reach out to support@convai.com. Join our [Discord server](https://discord.gg/5mRtu2WhEm) to connect with the Convai community and receive updates and support from fellow developers.

---

Thank you for choosing Convai for your development needs. We look forward to seeing the amazing experiences you create!

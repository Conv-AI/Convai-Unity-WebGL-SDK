var ConvaiUnityWebGL = {
    convaiClient: null,
    userTextStream: "",
    isTalking: false,
    interactionId: "",
    chatOutputPlaceholder: "Type your message here",

    initMicrophone: function () {
        var self = this;
        navigator.mediaDevices.getUserMedia({audio: true})
            .then(function (stream) {
                console.log('Microphone permission granted!');
                self.mediaStream = stream;
            })
            .catch(function (error) {
                console.log('Microphone permission denied:', error);
            });
    },

    initializeConvaiClient: function (apiKey, characterId, enableAudioRecorder, actionConfig, templateKeys) {
        let NDTemplateKeys;
        templateKeys = UTF8ToString(templateKeys);

        if (templateKeys) {
            const parsedData = JSON.parse(templateKeys);
            NDTemplateKeys = new Map();

            if (Array.isArray(parsedData.narrativeDesignKeys)) {
                parsedData.narrativeDesignKeys.forEach(item => {
                    if (item.name && item.value) {
                        NDTemplateKeys.set(item.name, item.value);
                    }
                });
            }

            NDTemplateKeys.forEach((value, key) => console.log("Key:", key, "Value:", value));
        }

        let ActionConfig;

        actionConfig = UTF8ToString(actionConfig);

        if (actionConfig) {
            ActionConfig = JSON.parse(actionConfig);
        }


        this.convaiClient = new ConvaiClient({
            apiKey: UTF8ToString(apiKey),
            characterId: UTF8ToString(characterId),
            enableAudio: enableAudioRecorder,
            faceModal: 3,
            enableFacialData: true,
            narrativeTemplateKeysMap: NDTemplateKeys && NDTemplateKeys.size > 0 ? NDTemplateKeys : undefined,
            actionConfig: ActionConfig ? ActionConfig : undefined
        });


        this.convaiClient.onAudioPlay(() => {
            if (!this.isTalking) {
                SendMessage("ConvaiGRPCWebAPI", "SetTalkingStatus", 'true');
                this.isTalking = true;
            }
        });

        this.convaiClient.onAudioStop(() => {
            if (this.isTalking) {
                SendMessage("ConvaiGRPCWebAPI", "SetTalkingStatus", 'false');
                this.isTalking = false;
            }
        });

        if (this.convaiClient.getAudioVolume() > 0) {
            this.convaiClient.toggleAudioVolume()
        }

        this.convaiClient.setErrorCallback(function (type, message) {
            var errorMessage = type + " : " + message;
            console.log("Error Message: ", errorMessage);
        })

        this.convaiClient.setResponseCallback(function (response) {
            if (response.hasUserQuery()) {
                var transcript = response.getUserQuery();
                var isFinal = transcript.getIsFinal();
                var transcriptText = transcript.getTextData();

                if (isFinal) {
                    this.userTextStream += transcriptText;
                    SendMessage("ConvaiGRPCWebAPI", "OnUserResponseReceived", this.userTextStream);
                }

                this.userTextStream = transcriptText;
                SendMessage("ConvaiGRPCWebAPI", "OnUserResponseReceived", this.userTextStream);
            }

            if (response.hasBtResponse()) {
                let btResponse = response.getBtResponse().getNarrativeSectionId();
                console.log("BT Response: ", btResponse)
                SendMessage("ConvaiGRPCWebAPI", "OnBTResponseReceived", btResponse);
            }

            if (response.hasActionResponse()) {
                let actionResponse = response.getActionResponse();
                //console.log("Action Response: ", actionResponse.getAction());
                SendMessage("ConvaiGRPCWebAPI", "OnActionResponseReceived", actionResponse.getAction());
            }
            if (response.hasAudioResponse()) {
                var audioResponse = response.getAudioResponse();
                var responseText = audioResponse.getTextData();

                if (responseText != "") {
                    //console.log("Response Text: " + responseText);
                    console.log("Session ID: ", response.getSessionId());
                    SendMessage("ConvaiGRPCWebAPI", "OnTextResponseReceived", responseText);
                }

                var visemeData;
                if (audioResponse.hasVisemesData()) {
                    visemeData = audioResponse.getVisemesData();
                    if (visemeData && visemeData.toObject) {
                        var visemeArray = visemeData.array[0]
                        SendMessage("ConvaiGRPCWebAPI", "OnVisemeResponseReceived", JSON.stringify(visemeArray));
                    }
                }
            }

            if (response && response.hasInteractionId()) {
                console.log("Interaction ID: ", response.getInteractionId());
                this.interactionId = response.getInteractionId();
            }
        });
    },

    startAudioChunk: function () {
        if (this.convaiClient !== null) {
            this.convaiClient.startAudioChunk();
        }
    },

    sendTextRequest: function (request) {
        //console.log("Request: " + UTF8ToString(request));
        this.convaiClient.sendTextChunk(UTF8ToString(request));
    },

    endAudioChunk: function () {
        if (this.convaiClient !== null) {
            this.convaiClient.endAudioChunk();
        }
    },

    toggleAudioVolume: function () {
        console.log("Toggling Audio Volume");
        this.convaiClient.toggleAudioVolume();
    },

    pauseAudio: function () {
        console.log("Pausing Audio");
        this.convaiClient.pauseAudio();
    },

    resumeAudio: function () {
        console.log("Resuming Audio");
        this.convaiClient.resumeAudio();
    },

    getAudioVolume: function () {
        const volume = this.convaiClient.getAudioVolume();
        console.log("Getting Audio Volume", volume);
        SendMessage("ConvaiGRPCWebAPI", "SetAudioVolume", volume);
    },
    sendTriggerData: function (name, message, preload = false) {
        console.log("%cInvoke Trigger Name : " + UTF8ToString(name) + " | Message: " + UTF8ToString(message) + " | PreLoad: " + preload, "color: pink;");
        this.convaiClient.invokeTrigger(UTF8ToString(name), UTF8ToString(message), preload)
    },

    setActionConfig: function (config) {
        var parsedConfig = JSON.parse(UTF8ToString(config));

        this.convaiClient.setActionConfig(parsedConfig);
    },

    interruptCharacter: function () {
        //console.log("Interrupting Character");
        this.convaiClient.stopCharacterAudio();
    },

    sendFeedback: function (character_id, session_id, thumbs_up, feedback_text) {
        console.log("Interaction id: " + this.interactionId);
        if (this.interactionId === "" || this.interactionId === null || this.interactionId === undefined) {
            return;
        }
        this.convaiClient.sendFeedback(this.interactionId, UTF8ToString(character_id), UTF8ToString(session_id), thumbs_up, UTF8ToString(feedback_text));
    },
};

mergeInto(LibraryManager.library, ConvaiUnityWebGL);

//console.log(
//    'OS: ' + jscd.os + ' ' + jscd.osVersion + '\n' +
//    'Browser: ' + jscd.browser + ' ' + jscd.browserMajorVersion +
//    ' (' + jscd.browserVersion + ')\n' +
//    'Mobile: ' + jscd.mobile + '\n' +
//    'Flash: ' + jscd.flashVersion + '\n' +
//    'Cookies: ' + jscd.cookies + '\n' +
//    'Screen Size: ' + jscd.screen + '\n\n' +
//    'Full User Agent: ' + navigator.userAgent + '\n\n' +
//    'IP: ' + ipInfo.ip + '\n' +
//    'City: ' + ipInfo.city + '\n' +
//    'Region: ' + ipInfo.region + '\n' +
//    'Latitude: ' + ipInfo.latitude + '\n' +
//    'Longtitude: ' + ipInfo.longtitude
//);
var recorder;
var audioFileFromServer = "";
$(document).ready(function () {
    window.URL = window.URL || window.webkitURL;
    /** 
     * Detecte the correct AudioContext for the browser 
     * */
    window.AudioContext = window.AudioContext || window.webkitAudioContext;
    navigator.getUserMedia = navigator.getUserMedia || navigator.webkitGetUserMedia || navigator.mozGetUserMedia || navigator.msGetUserMedia;
    let startBtn = document.getElementById('btn-on-micro');
    let stopBtn = document.querySelector('.btn_stop_audio');
    let stopBtn2 = document.getElementById('btn-submit-chat-message');
    recorder = new RecordVoiceAudios();
    startBtn.onclick = recorder.startRecord;
    stopBtn.onclick = recorder.stopRecord;
    stopBtn2.onclick = recorder.stopRecord;

    function RecordVoiceAudios() {
        let audioElement = document.querySelector('audio');
        let encoder = null;
        let microphone;
        let isRecording = false;
        var audioContext;
        let processor;
        let config = {
            bufferLen: 4096,
            numChannels: 2,
            mimeType: 'audio/mpeg'
        };

        this.startRecord = function () {
            audioContext = new AudioContext();
            /** 
            * Create a ScriptProcessorNode with a bufferSize of 
            * 4096 and two input and output channel 
            * */
            if (audioContext.createJavaScriptNode) {
                processor = audioContext.createJavaScriptNode(config.bufferLen, config.numChannels, config.numChannels);
            } else if (audioContext.createScriptProcessor) {
                processor = audioContext.createScriptProcessor(config.bufferLen, config.numChannels, config.numChannels);
            } else {
                console.log('WebAudio API has no support on this browser.');
                alert('WebAudio API has no support on this browser.')
                return false;
            }

            processor.connect(audioContext.destination);
            /**
            *  ask permission of the user for use microphone or camera  
            * */
            navigator.mediaDevices.getUserMedia({ audio: true, video: false })
                .then(gotStreamMethod)
                .catch(
                    logError
                );
        };

        let getBuffers = (event) => {
            var buffers = [];
            for (var ch = 0; ch < 2; ++ch)
                buffers[ch] = event.inputBuffer.getChannelData(ch);
            return buffers;
        }

        let gotStreamMethod = (stream) => {
            startBtn.setAttribute('disabled', true);
            stopBtn.removeAttribute('disabled');
            audioElement.src = "";
            config = {
                bufferLen: 4096,
                numChannels: 2,
                mimeType: 'audio/mpeg'
            };
            isRecording = true;

            let tracks = stream.getTracks();
            /** 
            * Create a MediaStreamAudioSourceNode for the microphone 
            * */
            microphone = audioContext.createMediaStreamSource(stream);
            /** 
            * connect the AudioBufferSourceNode to the gainNode 
            * */
            microphone.connect(processor);
            encoder = new Mp3LameEncoder(audioContext.sampleRate, 160);
            /** 
            * Give the node a function to process audio events 
            */
            processor.onaudioprocess = function (event) {
                encoder.encode(getBuffers(event));
            };

            stopBtnRecord = () => {
                audioFileFromServer = "";

                console.log('stopBtnRecord');
                isRecording = false;
                startBtn.removeAttribute('disabled');
                stopBtn.setAttribute('disabled', true);
                audioContext.close();
                processor.disconnect();
                tracks.forEach(track => track.stop());
                let blobs = encoder.finish('audio/wav');
                audioElement.src = URL.createObjectURL(blobs);                

                // pass variable outside ajax call
                audioFileFromServer = function (blobs) {
                    data = new FormData();
                    data.append('file', blobs);
                    $.ajax({
                        type: 'POST',
                        url: _Host + "api/livechat/import-audio",
                        async: false,//muốn pass data ra ngoài biến nên có asynce
                        global: false,
                        enctype: 'multipart/form-data',
                        processData: false,
                        contentType: false,
                        data: data,
                        success: function (result) {
                            console.log(result)
                            if (result.status) {
                                tmp = _Host + result.data;
                            }
                        }
                    });
                    return tmp;
                }(blobs);

                console.log(audioFileFromServer)
            };

            //analizer(audioContext);
        }

        this.stopRecord = function () {
            stopBtnRecord();
        };

        let analizer = (context) => {
            let listener = context.createAnalyser();
            microphone.connect(listener);
            listener.fftSize = 256;
            var bufferLength = listener.frequencyBinCount;
            let analyserData = new Uint8Array(bufferLength);

            let getVolume = () => {
                let volumeSum = 0;
                let volumeMax = 0;

                listener.getByteFrequencyData(analyserData);

                for (let i = 0; i < bufferLength; i++) {
                    volumeSum += analyserData[i];
                }

                let volume = volumeSum / bufferLength;

                if (volume > volumeMax)
                    volumeMax = volume;

                //drawAudio(volume / 10);
                /**
                * Call getVolume several time for catch the level until it stop the record
                */
                return setTimeout(() => {
                    if (isRecording)
                        getVolume();
                }, 10);
            }

            getVolume();
        }



        let logError = (error) => {
            $("#btn-remove-audio").addClass('d-none');
            $(".form-input-audio").addClass('d-none');

            $(".form-input-text").removeClass('d-none');
            $(".menu-footer").removeClass('d-none');
            $(".form-action-audio").removeClass('d-none');

            window.clearTimeout(timeOutAudio);
            document.getElementById('audio-realtime').innerHTML = "00:00";
            alert(error);
            console.log(error);
            return false;
        }
    }

})

var timeOutAudio;

var CustomerModel = {
    ID: _customerId,
    ApplicationChannels: 0, //[0: Web, 1: Facebook, 2: Zalo, 3: Kiosk]
    ChannelGroupID: _channelGroupId,
    ThreadID: '',
    BotID: '',
    Name: window.sessionStorage.getItem("lc_CustomerName") || '',
    Phone: window.sessionStorage.getItem("lc_CustomerPhone") || '',
    Email: window.sessionStorage.getItem("lc_CustomerEmail") || ''
};
//console.log(CustomerModel)
var AgentModel = {
    ID: '',
    ChannelGroupID: _channelGroupId,
    Status: '',
}
var DeviceModel = {
    ID: 0,
    OS: jscd.os + ' ' + jscd.osVersion,
    Browser: jscd.browser + ' ' + jscd.browserMajorVersion + ' (' + jscd.browserVersion + ')',
    IsMobile: jscd.mobile,
    FullUserAgent: navigator.userAgent,
    IPAddress: ipInfo.ip,
    City: ipInfo.city,
    Region: ipInfo.region,
    Latitude: ipInfo.latitude,
    Longtitude: ipInfo.longtitude
}


//----------------------------EVENT CHATBOX CUSTOMER ----------------------------//
/*
    
*/

var setting = {
    isAgentOnline: false,
    isTransferToBot: false,
    BotID: '',
    FirstCardID:''
}


var isTyping = false;

var TYPE_USER_CONNECT = {
    CUSTOMER: 'customer',
    AGENT: 'agent',
    BOT: 'bot'
}

var intervalReconnectId,
    timeReconnecting = 6;

var interval_focus_tab_id;

var objHub = $.connection.chatHub;
var firstClick = false;
$(function () {
    //check agent online
    setting.isAgentOnline = false;

    // Tạm thời để _channelGroupId thay botId
    setting.BotID = _botId;
    $("#header-title").html('Chatbot');
    setting.isTransferToBot = true;

    setting.FirstCardID = getCardInit().cardId;
});

$(document).ready(function () {
    // nếu lưu rồi k nên gọi vào tiếp
    _isSaveCustomer = window.sessionStorage.getItem("lc_isSaveCustomer");
    cBoxHub.init();
})

var varyReconnected = function intervalFunc() {
    timeReconnecting--;
    document.getElementById("reconeting-time").innerHTML = timeReconnecting;
    //console.log(timeReconnecting);
    if (timeReconnecting == parseInt("0")) {//timeReconnecting == 0
        clearInterval(intervalReconnectId);
        $('.box-reconecting').removeClass('showing');
        $(".chat-footer").show();
    }
}
var reconnectingTime = new Timer({
    minutes: 0,
    seconds: 5,
    element: document.querySelector('#reconeting-time')
});

var cBoxHub = {
    init: function () {
        cBoxHub.register();
        cBoxHub.receivedSignalFromServer();
        cBoxMessage.event();
    },
    register: function () {
        $.connection.hub.logging = true;
        $.connection.hub.qs = 'isCustomerConnected=true';
        $.connection.hub.start({ transport: ['longPolling', 'webSockets'] });
        $.connection.hub.start().done(function () {
            //console.log("signalr started")

            window.sessionStorage.setItem("lc_customerName", "bạn");
            window.sessionStorage.setItem("lc_isSaveCustomer", true);
            $(".message-form-user").hide();
            $(".chat-footer").show();

            // if (_isSaveCustomer == 'false') {
            // $(".chat-footer").hide();
            // $(".message-form-user").show();
            // }
            // else {
            // // kết nối chat khi agent hoặc bot active
            // if (setting.isAgentOnline == true) {
            // $(".chat-footer").show();
            // objHub.server.connectCustomerToChannelChat(_customerId, _channelGroupId);
            // }
            // else if (setting.isTransferToBot == true) {
            // $(".chat-footer").show();
            // new messageBot.renderTemplate('', '').Typing();
            // new messageBot.getMessage('postback_card_15169', '', setting.BotID);
            // //objHub.server.connectCustomerToChannelChat(_customerId, _channelGroupId);
            // }
            // else {
            // $(".chat-footer").hide();
            // }
            // }

            // start chat
            $('body').on('click', '#btn-starchat', function (e) {
                e.preventDefault();
                var customerName = $('#txtCustomerName').val(),
                    customerEmail = $('#txtCustomerEmail').val(),
                    customerPhone = $('#txtCustomerPhone').val()
                if (customerName == '') {
                    $('.cNameError').show();
                    return false;
                }
                CustomerModel.Name = customerName;
                CustomerModel.Email = customerEmail;
                CustomerModel.Phone = customerPhone;
                CustomerModel.BotID = setting.BotID;
                saveCustomer();
            })
        });

        let tryingToReconnect = false;

        $.connection.hub.error(function (error) {
            //console.log('SignalR error ngắt kết nối: ' + error)
            $(".box-disconected").addClass('showing');
            $(".chat-footer").hide();
            tryingToReconnect = true;
        });

        $.connection.hub.reconnecting(function () {
            tryingToReconnect = true;
            //console.log('SingalR connect đang quá trình kết nối')

        });

        $.connection.hub.reconnected(function () {
            tryingToReconnect = false;
            //console.log('SingalR connect thực hiện xong quá trình kết nối')
        });

        $.connection.hub.disconnected(function () {
            //console.log('SingalR connect ngắt kết nối')
            if ($.connection.hub.lastError) {
                $(".chat-footer").hide();
                $(".box-disconected").addClass('showing');
                console.log("Disconnected. Reason: " + $.connection.hub.lastError.message);
            }
            if (tryingToReconnect) {
                $(".box-disconected").removeClass('showing');
                $('.box-reconecting').addClass('showing');
                //intervalReconnectId = setInterval(varyReconnected, 1500);
                reconnectingTime.start();
                setTimeout(function () {
                    //console.log('SingalR connect đang khởi động lại')

                    // demo bỏ qua setting.isAgentOnline == true
                    $(".chat-footer").show();

                    $.connection.hub.start({ transport: ['longPolling', 'webSockets'] });
                    $.connection.hub.start().done(function () {
                        // kết nối chat khi agent hoặc bot active
                        //clearInterval(intervalReconnectId);
                        reconnectingTime.stop();
                        //$('.box-reconecting').removeClass('showing');
                        if (setting.isAgentOnline == true) {
                            $(".chat-footer").show();
                            objHub.server.connectCustomerToChannelChat(_customerId, _channelGroupId);
                        } else {
                            $(".chat-footer").hide();
                        }

                        //demo mặc định không có agent trả lời nên để hiện lại ô nhập tin nhắn
                        $(".chat-footer").show();
                    });
                }, 5000);		//Restart connection after 5 seconds.       		
            }
        });

        // gọi Onconnected
        objHub.client.connected = function () {
            //console.log('connected')
        }
        // gọi Onconnected
        objHub.client.disconnected = function () {
            //console.log('disconnected')
        }
        window.addEventListener("offline",
            () => console.log("No Internet")
        );
        window.addEventListener("online", function () {
            //console.log("Connected Interned")
            $(".box-disconected").removeClass('showing');
            $('.box-reconecting').addClass('showing');
            reconnectingTime.start();

            // intervalReconnectId = setInterval(varyReconnected, 1500);
            setTimeout(function () {
                //console.log('SingalR connect đang khởi động lại')
                $.connection.hub.start({ transport: ['longPolling', 'webSockets'] });
                $.connection.hub.start().done(function () {
                    // kết nối chat khi agent hoặc bot active
                    reconnectingTime.stop();
                    //clearInterval(intervalReconnectId);
                    //$('.box-reconecting').removeClass('showing');
                    if (setting.isAgentOnline == true) {
                        $(".chat-footer").show();
                        objHub.server.connectCustomerToChannelChat(_customerId, _channelGroupId);
                    } else {
                        $(".chat-footer").hide();
                    }

                    //demo mặc định không có agent trả lời nên để hiện lại ô nhập tin nhắn
                    $(".chat-footer").show();
                });
            }, 5000);
        });
    },
    receivedSignalFromServer: function () {
        objHub.client.receiveMessages = function (channelGroupId, threadId, message, agentId, customerId, agentName, typeUser) {
            //console.log('threadId:' + threadId + '  agentId-agentName' + agentId + ' : ' + message)

            //var $elementMessage = document.getElementsByClassName('message-item');
            //if ($elementMessage.length <= 1) {
            //    parent.postMessage("open", "*");
            //}

            insertChat("agent", isValidURLandCodeIcon(message), agentName, "");

        };
        objHub.client.receiveTyping = function (channelGroupId, threadId, agentId, agentName, customerId, isTyping, typeUser) {
            if (CustomerModel.ID == customerId) {
                if (isTyping) {
                    new messageUser.getTypingUser('');
                } else {
                    //remove typing
                    $(".message-item-typing").remove();
                }
                //console.log('agentId-' + agentId + ' typing')
            }
        };
        objHub.client.receiveThreadChat = function (threadId, customerId, conversationId) {
            //console.log('revice- thread' + threadId)
            CustomerModel.ThreadID = threadId;
        };
        objHub.client.receiveSingalChatWithBot = function (channelGroupId, threadId, customerId, botId, isTransfer) {
            if (isTransfer) {
                //console.log('revice- signal chat with bot' + botId)
                CustomerModel.ThreadID = threadId;
                setting.isTransferToBot = true;
                setting.BotID = botId;
                new messageBot.renderTemplate('', '').Typing();
                new messageBot.getMessage('postback_card_15169', CustomerModel.ThreadID, setting.BotID);
            } else {
                setting.isTransferToBot = false;
                setting.BotID = botId;
            }

        };
        objHub.client.receiveActionChat = function (hannelGroupId, threadId, contentAction, agentId, customerId) {
            insertActionChat(contentAction);
        };

        objHub.client.receiveSignalAgentFocusTabChat = function (channelGroupId, threadId, customerId, isFocusTab) {
            if (threadId != '') {
                if (CustomerModel.ID == customerId) {
                    $('#message-content').find('.message-item-action').html("Read");
                }
            }
        };

    },
    //validateFocusTabChat: function () {
    //    // Kiểm tra customer có hoạt dộng trên tab trình duyệt chat
    //    if (CustomerModel.ThreadID != '') {
    //        $(window).focus(function () {
    //            if (!interval_focus_tab_id) {
    //                interval_focus_tab_id = setInterval(function () {
    //                    console.log("customer hoat dong");
    //                    let isFocusTab = true;
    //                    objHub.server.checkCustomerFocusTabChat(_channelGroupId, CustomerModel.ThreadID, CustomerModel.ID, isFocusTab);
    //                    clearInterval(interval_focus_tab_id);
    //                }, 1000);
    //            }
    //        });
    //        // Nếu không xem màn hình
    //        $(window).blur(function () {
    //            clearInterval(interval_focus_tab_id);
    //            interval_focus_tab_id = 0;
    //            let isFocusTab = false;
    //            objHub.server.checkCustomerFocusTabChat(_channelGroupId, CustomerModel.ThreadID, CustomerModel.ID, isFocusTab);
    //            console.log("customer k hoat dong")
    //        });
    //    }
    //}
}

function saveCustomer() {
    var formData = new FormData();
    formData.append('customer', JSON.stringify(CustomerModel));
    formData.append('device', JSON.stringify(DeviceModel));
    $.ajax({
        url: _Host + "api/lc_customer/create",
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        dataType: "json",
        success: function (result) {
            if (result) {
                window.sessionStorage.setItem("lc_customerName", CustomerModel.Name);
                window.sessionStorage.setItem("lc_customerEmail", CustomerModel.Email);
                window.sessionStorage.setItem("lc_customerPhone", CustomerModel.Phone);
                window.sessionStorage.setItem("lc_isSaveCustomer", true);
                $(".message-form-user").hide();
                $(".chat-footer").show();

                if (setting.isAgentOnline == true) {
                    objHub.server.connectCustomerToChannelChat(_customerId, _channelGroupId);
                }
                else if (setting.isTransferToBot == true) {
                    $(".chat-footer").show();
                    new messageBot.renderTemplate('', '').Typing();
                    new messageBot.getMessage('postback_card_15169', '', setting.BotID);
                    //objHub.server.connectCustomerToChannelChat(_customerId, _channelGroupId);
                }
                else {
                    $(".chat-footer").hide();
                }
            }
        },
        error: function (e) {
            //console.log(e)
        }
    });
};

var checkAgentOnline = function () {
    var isCheck = false;
    var params = {
        channelGroupId: _channelGroupId
    }
    params = JSON.stringify(params);
    $.ajax({
        url: _Host + "api/lc_agent/getListAgentOnline",
        contentType: 'application/json; charset=utf-8',
        data: params,
        async: false,
        global: false,
        type: 'POST',
        success: function (data) {
            if (data.length != 0) {
                isCheck = true;
            }
        }
    })
    return isCheck;
}
var checkBotActive = function () {
    var chatBot = {
        botId: '',
        isActive: false,
        isTransfer: false
    };

    var params = {
        channelGroupId: _channelGroupId
    }
    params = JSON.stringify(params);
    $.ajax({
        url: _Host + "api/bot/getBotActiveLiveChat",
        contentType: 'application/json; charset=utf-8',
        data: params,
        async: false,
        global: false,
        type: 'POST',
        success: function (data) {
            if (data.status) {
                chatBot.botId = data.botId;
                chatBot.isActive = true;
            }
        }
    })
    return chatBot;
}

var getCardInit = function () {
    var card = {
        cardId : ''
    };

    var params = {
        botId: _botId
    }
    //params = JSON.stringify(params);
    $.ajax({
        url: _Host + "api/setting/getbybotid",
        contentType: 'application/json; charset=utf-8',
        data: params,
        async: false,
        global: false,
        type: 'GET',
        success: function (data) {
            card.cardId = data.CardID;
        }
    })
    return card;
}


// EVENT BOX
var cBoxMessage = {
    init: function () {

    },
    event: function () {
        // close box
        $('body').on('click', '#btn-cbox-close', function (e) {
            parent.postMessage("close", "*");
        })

        $("body").on('click', '.btn_next_genetics', function () {
            var $form = $(this).closest('.message-item-genetics'),
                currentIndex = $form.find($('div.message-container')).attr('index');
            minIndex = (parseInt(currentIndex) + 1),
                maxIndex = $form.find($('div.message-item-template-generic')).length - 1;
            $form.find($('div.message-container')).attr('index', minIndex);

            var widthByItem = -272 * minIndex,
                withScroll = '' + widthByItem + 'px';

            $form.find('.message-template').css('left', withScroll);
            $form.find('.btn_back_genetics').css('display', 'block');
            if (minIndex == maxIndex) {
                $form.find('.btn_next_genetics').css('display', 'none');
            }
        })
        $("body").on('click', '.btn_back_genetics', function () {
            var $form = $(this).closest('.message-item-genetics'),
                currentIndex = $form.find($('div.message-container')).attr('index'),
                index = (parseInt(currentIndex) - 1);

            $form.find($('div.message-container')).attr('index', index);

            var widthByItem = -272 * index,
                withScroll = '' + widthByItem + 'px';

            $form.find('.message-template').css('left', withScroll);
            if (index == 0) {
                $form.find('.btn_back_genetics').css('display', 'none');
            }
            $form.find('.btn_next_genetics').css('display', 'block');
        })

        $('body').on('click', '.message-btn-postback', function (e) {
            var dataPostback = $(this).attr('data-postback');
            var dataText = $(this).html();
            insertChat("customer", dataText, CustomerModel.Name, "");

            if (CustomerModel.ThreadID != "") {
                // gửi tin nhắn
                objHub.server.sendMessage(_channelGroupId, CustomerModel.ThreadID, dataText, "", CustomerModel.ID, "", TYPE_USER_CONNECT.CUSTOMER);
            }


            // bot xử lý
            new messageBot.getMessage(dataPostback, CustomerModel.ThreadID, setting.BotID);
        })

        $('body').on('click', '.btn-quickreply', function (e) {
            var dataPostback = $(this).attr('data-postback');
            var dataText = $(this).html();
            insertChat("customer", dataText, CustomerModel.Name, "");
            if (CustomerModel.ThreadID != "") {
                // gửi tin nhắn
                objHub.server.sendMessage(_channelGroupId, CustomerModel.ThreadID, dataText, "", CustomerModel.ID, "", TYPE_USER_CONNECT.CUSTOMER);
            }

            new messageBot.getMessage(dataPostback, CustomerModel.ThreadID, setting.BotID);
        })

        // click link file
        $('body').on('click', '.btn-link-file', function (e) {
            let link = $(this).attr('href');
            let message = "OPEN_WORD_OFFICE:" + $(this).attr('href');
            parent.postMessage(message, "*");
        })

        $("#input-chat-message").focus(function (e) {

            // Ẩn button menu footer nếu đang hiển thị
            if ($("._661n_btn_menu_chat").attr('aria-expanded') == 'true') {
                $("._661n_btn_menu_chat").dropdown('toggle');
            }
            if (CustomerModel.ThreadID == "") {
                return false;
            }
            if (!interval_focus_tab_id) {
                interval_focus_tab_id = setInterval(function () {
                    //console.log("customer click focus");
                    let isFocusTab = true;
                    objHub.server.checkCustomerFocusTabChat(_channelGroupId, CustomerModel.ThreadID, CustomerModel.ID, isFocusTab);
                    clearInterval(interval_focus_tab_id);
                }, 1000);
            }
        })
        $("#input-chat-message").blur(function (e) {
            clearInterval(interval_focus_tab_id);
            interval_focus_tab_id = 0;
            let isFocusTab = false;
            if (CustomerModel.ThreadID == "")
                return false;

            objHub.server.checkCustomerFocusTabChat(_channelGroupId, CustomerModel.ThreadID, CustomerModel.ID, isFocusTab);
            //console.log("customer ngoài focus")
        })


        $("#input-chat-message").keyup(function (e) {
            var edValue = $(this);
            var text = edValue.val();
            if (CustomerModel.ThreadID == "") {
                return false;
            }
            if (text.length > 0) {
                //$(".btn_submit").show();
                if (isTyping == false) {
                    isTyping = true;
                    objHub.server.sendTyping(_channelGroupId, CustomerModel.ThreadID, '', '', CustomerModel.ID, isTyping, TYPE_USER_CONNECT.CUSTOMER);
                }
            } else {
                isTyping = false;
                objHub.server.sendTyping(_channelGroupId, CustomerModel.ThreadID, '', '', CustomerModel.ID, isTyping, TYPE_USER_CONNECT.CUSTOMER);
                //$(".btn_submit").hide();
            }
        })

        $("#input-chat-message").keydown(function (e) {
            var edValue = $(this);
            var text = edValue.val();
            if (e.which == 13) {
                e.preventDefault(e);
                if (text !== "") {
                    insertChat("customer", isValidURLandCodeIcon(text), CustomerModel.Name, "");
                    if (CustomerModel.ThreadID != "") {
                        // gửi tin nhắn
                        objHub.server.sendMessage(_channelGroupId, CustomerModel.ThreadID, text, "", CustomerModel.ID, "", TYPE_USER_CONNECT.CUSTOMER);
                    }

                    $(this).val('');

                    if (setting.isTransferToBot == true) {
                        new messageBot.renderTemplate('', '').Typing();
                        new messageBot.getMessage(text, CustomerModel.ThreadID, setting.BotID);
                    }
                    isTyping = false;

                    //$(".btn_submit").hide();

                    return;
                }
            }
        })

        $("#btn-submit-chat-message").on('click', function () {
            var edValue = $("#input-chat-message");
            var text = edValue.val();
            //console.log(text)
            if (text !== "") {
                insertChat("customer", isValidURLandCodeIcon(text), CustomerModel.Name, '');

                if (CustomerModel.ThreadID != "") {
                    // gửi tin nhắn
                    objHub.server.sendMessage(_channelGroupId, CustomerModel.ThreadID, text, "", CustomerModel.ID, "", TYPE_USER_CONNECT.CUSTOMER);
                }

                // emty input
                $("#input-chat-message").val('');

                if (setting.isTransferToBot == true) {
                    new messageBot.renderTemplate('', '').Typing();
                    new messageBot.getMessage(text, CustomerModel.ThreadID, setting.BotID);
                }
            }

            if (setting.isTransferToBot == true) {
                if (text == "") {
                    if (!$(".form-input-audio").hasClass('d-none')) {
                        let srcAudio = $("#file-audio").attr('src');
                        let fileAudio = '<audio class="message-audio" controls="" type="audio/mpeg" src="' + srcAudio + '" style="width:160px;height:40px"></audio>';
                        insertChat("customer", fileAudio, CustomerModel.Name, '');

                        if (audioFileFromServer != "") {
                            new messageBot.renderTemplate('', '').Typing();
                            new messageBot.getMessage(audioFileFromServer, CustomerModel.ThreadID, setting.BotID);
                        }
                    }
                    resetAudio();
                }                
            }
                       
            return;
        })

        // footer button menu
        $('body').on('click', '.footer-menu__btn--postback', function (e) {
            var dataPostback = $(this).attr('data-postback');
            var dataText = $(this).html();
            insertChat("customer", dataText, CustomerModel.Name, "");

            if (CustomerModel.ThreadID != "") {

                // gửi tin nhắn
                objHub.server.sendMessage(_channelGroupId, CustomerModel.ThreadID, dataText, "", CustomerModel.ID, "", TYPE_USER_CONNECT.CUSTOMER);
            }

            // bot xử lý
            new messageBot.getMessage(dataPostback, CustomerModel.ThreadID, setting.BotID);
        })

        $('body').on('click', '#btn-on-micro', function () {
            $("#input-chat-message").val('');
            $(".form-input-text").addClass('d-none');
            $(".menu-footer").addClass('d-none');
            $(".form-action-audio").addClass('d-none');

            $("#btn-remove-audio").removeClass('d-none');
            $(".form-input-audio").removeClass('d-none');

            format_time_audio(1);
        })

        $("body").on('click', '#btn-remove-audio', function () {
            resetAudio();
        })
        function format_time_audio(audio_duration) {
            sec = Math.floor(audio_duration);
            min = Math.floor(sec / 60);
            min = min >= 10 ? min : '0' + min;
            sec = Math.floor(sec % 60);
            sec = sec >= 10 ? sec : '0' + sec;

            document.getElementById('audio-realtime').innerHTML = min + ":" + sec;
            timeOutAudio = setTimeout(function () {
                let increasTime = parseInt(audio_duration) + 1;
                format_time_audio(increasTime)
            }, 1000);

            //return min + ":" + sec;
        }
    },
    callAction: function () {
        this.sendMessage = function () {
        }
    }
    // call api
    // render template
}

function insertActionChat(contentAction) {
    var htmlAction = '<div class="message-item message-item-divider clearfix">';
    htmlAction += '<span>' + contentAction + '</span>';
    htmlAction += '    </div>';
    $("#message-content").append(htmlAction)
    // scroll to bottom
    scrollBar();
}


function insertChat(who, text, userName, avatar) {
    //remove typing
    $(".message-quickreply").remove();
    $(".message-item-typing").remove();

    let user_class_chat = (who == "customer" ? "me" : "agent");
    let date_current = showTimeChat();

    var $elementMessage = document.getElementsByClassName('message-item');
    if ($elementMessage !== undefined || $elementMessage !== null) {
        let $elementLastMessage = $($elementMessage[$elementMessage.length - 1]);
        let timeLastMessage = $elementLastMessage.find('.message-user-time').html();
        let identity_user = $elementLastMessage.attr('data-user');
        if ((identity_user == who) && (date_current == timeLastMessage)) {
            let elementLastMessageAppend = $elementLastMessage.find('.message-item-content').last();
            appendMessage(elementLastMessageAppend, who, text);
            return;
        }
    }

    content = '<div class="message-item ' + user_class_chat + ' clearfix" data-user="' + who + '">';
    content += messageUser.getAgentAvatar(avatar);
    content += messageUser.getHtmlMessageBody(who, userName, text, date_current);
    content += '</div>';

    // insert body chat
    messageUser.appendMessage("", who, content);

    $(".message-audio").each(function (index) {
        $(this).parent().css('background-color', 'white').css('border', '0');
    })
    return false;
}

function appendMessage(elementLastMessageAppend, who, text) {
    if (elementLastMessageAppend !== "") {
        var content = '<div class="message-item-content">' + text + '</div>';
        $(elementLastMessageAppend).after($(content).hide().fadeIn(300));
    } else {
        $("#message-content").append(text).children(':last').hide().fadeIn(300);
    }
    // remove action read, delivered, message not send
    if (who == TYPE_USER_CONNECT.AGENT) {
        $("#message-content").find('.message-item-action').remove();
    }
    // scroll to bottom
    scrollBar();

}

var messageUser = {
    getAgentAvatar: function (avatar) {
        var templateAvatar = '';
        templateAvatar += '<div class="message-avatar">';
        templateAvatar += '<div class="message-avatar-customer">';
        templateAvatar += '<div class="pr-3">';
        templateAvatar += '<span class ="message-avatar-item avatar">';
        if (avatar == "") {
            templateAvatar += '<img src="' + _Host + 'assets/livechat/images/user_agent-default.png" class="rounded-circle" alt="image">';
        } else {
            templateAvatar += '<img src="~/assets/client/img/avatar-admin.jpg" class="rounded-circle" alt="image">';
        }
        templateAvatar += '</span>';
        templateAvatar += '</div>';
        templateAvatar += '</div>';
        templateAvatar += '</div>';
        return templateAvatar;
    },
    getHtmlMessageBody: function (who, userName, text, date_current) {
        var templateMsg = '';
        templateMsg += '<div class="message-body">';
        templateMsg += '<div>';
        templateMsg += '<div class ="message-align">';
        templateMsg += '<span class="message-user-name font-size-08">' + userName + ' </span>';
        templateMsg += '<span class="message-user-time font-size-08">' + date_current + '</span>';
        templateMsg += '</div>';
        templateMsg += '</div>';
        templateMsg += '<div class="message-item-content">' + text + '</div>';
        if (who == "customer") {
            templateMsg += '<div class="message-item-action txt-align-left font-size-08">Delivered</div>';//Message not sent, , Read
        }
        templateMsg += '</div>';
        return templateMsg;
    },
    getTypingUser: function (avatar) {
        var tmpText = '<div class="message-item message-item-typing clearfix">';
        tmpText += messageUser.getAgentAvatar(avatar)
        tmpText += `<div class="message-item-content writing">
                                <div class="_4xko _13y8">
                                    <div class="_4a0v _1x3z">
                                        <div class="_4b0g">
                                            <div class="_5pd7"></div>
                                            <div class="_5pd7"></div>
                                            <div class="_5pd7"></div>
                                        </div>
                                    </div>
                                </div>
                          </div>`;
        tmpText += '</div>';
        $('#message-content').append(tmpText).children(':last').hide().fadeIn(300);
        scrollBar();
    },
    appendMessage: function (elementLastMessageAppend, who, text) {
        if (elementLastMessageAppend !== "") {
            var content = '<div class="message-item-content">' + text + '</div>';
            $(elementLastMessageAppend).after($(content).hide().fadeIn(300));
        } else {
            $("#message-content").append(text).children(':last').hide().fadeIn(300);
        }
        // remove action read, delivered, message not send
        if (who == TYPE_USER_CONNECT.AGENT) {
            $("#message-content").find('.message-item-action').remove();
        }
        // scroll to bottom
        scrollBar();
    }
}
var messageBot = {
    getMessage: function (text, threadId, botId) {
        var params = {
            senderId: _customerId,
            botId: botId,
            text: text,
            channelGroupId: _channelGroupId,
            threadId: threadId
        }
        params = JSON.stringify(params);
        $.ajax({
            url: _Host + "api/livechat/receive",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: params,
            type: 'POST',
            success: function (response) {
                console.log(response)
                if (response.status == 2) {
                    var lstTemplateHtml = [];
                    let date_current = showTimeChat();
                    $.each(response.data, function (index, value) {
                        if (value.message.text != undefined) {
                            let tempText = new messageBot.renderTemplate(date_current, '').Text(value.message.text);
                            lstTemplateHtml.push(tempText);
                        }
                        if (value.message.attachment != undefined) {
                            if (value.message.attachment.type == "image") {
                                let tempImage = new messageBot.renderTemplate(date_current, '').Image(value.message.attachment.payload.url);
                                lstTemplateHtml.push(tempImage);
                            }
                            if (value.message.attachment.type == "file") {
                                let tempImage = new messageBot.renderTemplate(date_current, '').File(value.message.attachment.payload.url);
                                lstTemplateHtml.push(tempImage);
                            }
                            if (value.message.attachment.type == "template") {
                                if (value.message.attachment.payload.template_type == "button") {
                                    let text = value.message.attachment.payload.text;
                                    var lstButton = value.message.attachment.payload.buttons;
                                    let tempTextAndButton = new messageBot.renderTemplate(date_current, '').TextAndButton(text, function () {
                                        let htmlButton = new messageBot.renderTemplate(date_current, '').Button(lstButton);
                                        return htmlButton;
                                    });
                                    lstTemplateHtml.push(tempTextAndButton);
                                }
                                if (value.message.attachment.payload.template_type == "generic") {
                                    let templateGenerics = '';
                                    var templateGenericIndex = '';
                                    let lstGeneric = value.message.attachment.payload.elements;
                                    $.each(lstGeneric, function (index, value) {
                                        let genetic = {};
                                        genetic.title = value.title;
                                        genetic.item_url = value.item_url;//(value.item_url == "" ? "" : value.item_url.substr(value.item_url.indexOf('://') + 3));
                                        genetic.image_url = value.image_url; //_Host + 
                                        genetic.subtitle = value.subtitle;
                                        var lstButton = value.buttons;
                                        templateGenericIndex += new messageBot.renderTemplate(date_current, '').GenericIndex(genetic, function () {
                                            let htmlButton = new messageBot.renderTemplate(date_current, '').Button(lstButton);
                                            return htmlButton;
                                        });
                                    })

                                    templateGenerics = new messageBot.renderTemplate(date_current, '').ContainerGeneric(templateGenericIndex, lstGeneric.length);

                                    lstTemplateHtml.push(templateGenerics);
                                }
                            }
                        }
                        if (value.message.quick_replies != undefined) {
                            let tempQuickReply = new messageBot.renderTemplate(date_current, '').QuickReply(value.message.quick_replies)
                            lstTemplateHtml.push(tempQuickReply);
                        }
                    });
                    if (lstTemplateHtml.length != 0) {
                        $.each(lstTemplateHtml, function (index, value) {
                            let timeAppend = 500 * (index + 1);
                            let typing = true;
                            // nếu append tin nhắn cuối cùng từ 2 tin chở lên thì hủy typing
                            if (lstTemplateHtml.length == (index + 1)) {
                                typing = false;
                            }
                            new messageBot.appendMessage('#message-content', timeAppend, value, typing)
                            // gửi tin vai trò hiển thị của BOT trong chatroom
                        })
                    }
                } else {
                    $(".message-item-typing").remove();
                }
            }
        })
    },
    appendMessage: function (element, timeout, text, isSendTyping) {
        isSendTyping = isSendTyping || false;
        $(element).delay(timeout).queue(function (next) {
            $("#message-content").find('.message-item-action').remove();
            $(".message-item-typing").remove();
            $(this).append($(text).addClass('animated moveUp')).append(scrollBar());
            if (isSendTyping) {
                new messageBot.renderTemplate('', '').Typing();
            }

            if (CustomerModel.ThreadID != "") {
                objHub.server.sendMessage(_channelGroupId, CustomerModel.ThreadID, text, "", CustomerModel.ID, "", TYPE_USER_CONNECT.BOT);
            }

            next();
        });
    },
    getIcon: function (avatarBot) {
        var templateAvatar = '';
        templateAvatar += '<div class="message-avatar">';
        templateAvatar += '<div class="message-avatar-customer">';
        templateAvatar += '<div class="pr-3">';
        templateAvatar += '<span class ="message-avatar-item avatar">';
        templateAvatar += '<img src="' + _Host + 'assets/images/logo/icon-bot-lc.png" class="rounded-circle" alt="image">';
        templateAvatar += '</span>';
        templateAvatar += '</div>';
        templateAvatar += '</div>';
        templateAvatar += '</div>';
        return templateAvatar;
    },
    renderTemplate: function (date_current, avatarBot) {
        function getAvatar() {
            var templateAvatar = '';
            templateAvatar += '<div class="message-avatar">';
            templateAvatar += '<div class="message-avatar-customer">';
            templateAvatar += '<div class="pr-3">';
            templateAvatar += '<span class ="message-avatar-item avatar">';
            templateAvatar += '<img src="' + _Host + 'assets/images/logo/icon-bot-lc.png" class="rounded-circle" alt="image">';
            templateAvatar += '</span>';
            templateAvatar += '</div>';
            templateAvatar += '</div>';
            templateAvatar += '</div>';
            return templateAvatar;
        }
        this.Typing = function () {
            $(".message-quickreply").remove();
            var tmpText = '<div class="message-item message-item-typing clearfix">';
            tmpText += messageBot.getIcon('')
            tmpText += `<div class="message-item-content writing">
                                <div class="_4xko _13y8">
                                    <div class="_4a0v _1x3z">
                                        <div class="_4b0g">
                                            <div class="_5pd7"></div>
                                            <div class="_5pd7"></div>
                                            <div class="_5pd7"></div>
                                        </div>
                                    </div>
                                </div>
                          </div>`;
            tmpText += '</div>';
            $('#message-content').append(tmpText);//$('#message-content').append(tmpText).append(scrollBar())
        },
            this.Text = function (text) {
                var tmpText = '<div class="message-item {bot} message-item-text clearfix">';
                tmpText += messageBot.getIcon('');
                tmpText += '<div class="message-body">';
                if (setting.BotID != "5041") {
                    tmpText += '                    <div>';
                    tmpText += '                        <div class="message-align">';
                    tmpText += '                            <span class="message-user-name font-size-08">Bot </span>';
                    tmpText += '                            <span class="message-user-time font-size-08">' + date_current + '</span>';
                    tmpText += '                        </div>';
                    tmpText += '                    </div>';
                }
                tmpText += '                    <div class="message-item-content">' + text + '</div>';
                tmpText += '                </div>';
                tmpText += '</div>';
                return tmpText;
            },
            this.File = function (urlFile) {
                let iconFile = '<i style="color:#007bff" class="fa fa-file"></i>';
                let host = _Host + "File/Document/";
                let fileName = urlFile.replace(host, "");
                let fileExtension = fileName.split('.').pop();
                if (fileExtension == "docx" || fileExtension == "doc") {
                    iconFile = '<i style="color:#007bff" class="fa fa-file-word"></i>';
                }

                if (fileExtension == "pdf") {
                    iconFile = '<i style="color:red" class="fa fa-file-pdf"></i>';
                }

                var tmpText = '<div class="message-item {bot} message-item-text clearfix">';
                tmpText += messageBot.getIcon('');
                tmpText += '<div class="message-body">';
                if (setting.BotID != "5041") {
                    tmpText += '                    <div>';
                    tmpText += '                        <div class="message-align">';
                    tmpText += '                            <span class="message-user-name font-size-08">Bot </span>';
                    tmpText += '                            <span class="message-user-time font-size-08">' + date_current + '</span>';
                    tmpText += '                        </div>';
                    tmpText += '                    </div>';
                }
                tmpText += '                    <div class="message-item-content"> ' + iconFile + ' <a class="btn-link-file" href="' + urlFile + '" target="_blank"> ' + fileName + '</a></div>';
                tmpText += '                </div>';
                tmpText += '</div>';
                return tmpText;
            },
            this.Image = function (urlImage) {
                var tmpText = '<div class="message-item {bot} message-item-image clearfix">';
                tmpText += messageBot.getIcon(avatarBot);
                tmpText += '<div class="message-body">';
                if (setting.BotID != "5041") {
                    tmpText += '                    <div>';
                    tmpText += '                        <div class="message-align">';
                    tmpText += '                            <span class="message-user-name font-size-08">Bot </span>';
                    tmpText += '                            <span class="message-user-time font-size-08">' + date_current + '</span>';
                    tmpText += '                        </div>';
                    tmpText += '                    </div>';
                }
                tmpText += '                    <img src="' + urlImage + '"/>';//' + _Host + '
                tmpText += '                </div>';
                tmpText += '</div>';
                return tmpText;
            },
            this.TextAndButton = function (text, calbackButton) {
                var tmpText = '<div class="message-item {bot} message-item-text-button clearfix">';
                tmpText += messageBot.getIcon(avatarBot);
                tmpText += '<div class="message-body">';
                if (setting.BotID != "5041") {
                    tmpText += '                    <div>';
                    tmpText += '                        <div class="message-align">';
                    tmpText += '                            <span class="message-user-name font-size-08">Bot </span>';
                    tmpText += '                            <span class="message-user-time font-size-08">' + date_current + '</span>';
                    tmpText += '                        </div>';
                    tmpText += '                    </div>';
                }
                tmpText += '                    <div class="message-item-content">';
                tmpText += '<span>' + text + '</span>';
                tmpText += calbackButton();
                tmpText += '                    </div>';
                tmpText += '  </div>';
                tmpText += '  </div>';
                return tmpText;
            },
            this.ContainerGeneric = function (templateGenericIndex, genericTotal) {
                var tmpText = '<div class="message-item {bot} message-item-genetics clearfix">';
                tmpText += messageBot.getIcon(avatarBot);
                tmpText += '     <div class="message-body">';
                if (setting.BotID != "5041") {
                    tmpText += '                    <div>';
                    tmpText += '                        <div class="message-align">';
                    tmpText += '                            <span class="message-user-name font-size-08">Bot </span>';
                    tmpText += '                            <span class="message-user-time font-size-08">' + date_current + '</span>';
                    tmpText += '                        </div>';
                    tmpText += '                    </div>';
                }



                tmpText += '                <div class="message-container" index="0">';//style="padding-top:15px;"
                tmpText += '                                   <div class="message-template" style="left: 0;position: relative;transition: left 500ms ease-out;white-space: nowrap; display: flex; width: 100%; flex-direction: row;">';
                tmpText += templateGenericIndex;
                tmpText += '                                   </div>';
                tmpText += '                </div>';
                tmpText += '     </div>';
                if (genericTotal > 1) {
                    tmpText += ` <a class="btn_back_genetics _32rk _32rg _1cy6" href="#" style="display: none;">
                                <div direction="forward" class="_10sf _5x5_">
                                    <div class="_5x6d">
                                        <div class="_3bwv _3bww">
                                            <div class="_3bwy">
                                                <div class="_3bwx">
                                                    <i class="_3-8w img sp_RQ3p_x3xMG2 sx_c4c7bc" alt=""></i>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </a>
                            <a class ="btn_next_genetics _32rk _32rh _1cy6" href="#" style="display: block;">
                                <div direction="forward" class="_10sf _5x5_">
                                    <div class="_5x6d">
                                        <div class="_3bwv _3bww">
                                            <div class="_3bwy">
                                                <div class="_3bwx">
                                                    <i class="_3-8w img sp_RQ3p_x3xMG3 sx_dbbd74" alt=""></i>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </a>`;
                }
                tmpText += '</div>';
                return tmpText;
            },
            this.GenericIndex = function (generic, calbackButton) {
            var tmpText = '<div class="message-item-template-generic">';
            if (generic.image_url.includes("faq_legal")) {
                tmpText += '                               <div class="message-banner _6j0s" style="background-image: url(' + generic.image_url + '); background-position: center center; height: 150px; width: 100%;display:none">';

            } else {
                tmpText += '                               <div class="message-banner _6j0s" style="background-image: url(' + generic.image_url + '); background-position: center center; height: 150px; width: 100%;">';

            }
                tmpText += '                                </div>';
                tmpText += '                                <div class="message-template-generic-container _6j2g">';
                tmpText += '                                    <div class="message-generic-title _6j0t _4ik4 _4ik5" style="-webkit-line-clamp: 3;">';
                tmpText += '                                        ' + generic.title + ''
                tmpText += '                                    </div>';
                tmpText += '                                    <div class="message-generic-subtitle _6j0u _4ik4">';
                tmpText += '                                        <div>';
                tmpText += '                                            ' + generic.subtitle + ''
                tmpText += '                                        </div>';
                tmpText += '                                    </div>';
                tmpText += '                                    <div class="message-generic-sublink _6j0y _4ik4">';
                tmpText += '                                        <a href="' + generic.item_url + '" target="_blank">';
                tmpText += '                                            ' + generic.item_url + ''
                tmpText += '                                        </a>';
                tmpText += '                                    </div>';
                tmpText += '                                </div>';
                tmpText += calbackButton();
                tmpText += '  </div>';
                return tmpText;
            },
            this.Button = function (lstButton) {
                var tmpText = '';
                if (lstButton.length > 0) {
                    tmpText = '<div class="message-item-button">';
                    $.each(lstButton, function (index, value) {
                        if (value.type == "postback") {
                            tmpText += '<a class="message-btn-postback lc-6qcmqf" data-postback="' + value.payload + '">' + value.title + '</a>';
                        }
                        if (value.type == "web_url") {
                            tmpText += '<a href="' + value.url + '" class="message-btn-link lc-6qcmqf" target="_blank">' + value.title + '</a>';
                        }
                    })
                    tmpText += '</div>';
                }
                return tmpText;
            },
            this.QuickReply = function (lstQuickReply) {
                var tmpText = '        <div class="message-quickreply clearfix">';
                tmpText += '                <div class="message-quickreply-container">';
                tmpText += '                    <div class="message-quickreply-item" style="position:relative;" index="2">';
                $.each(lstQuickReply, function (index, value) {
                    tmpText += '                        <button value="0" class="btn-quickreply" data-postback="' + value.payload + '">' + value.title + '</button>';
                })
                tmpText += '                    </div>';
                tmpText += '                </div>';
                if (lstQuickReply.length > 3) {
                    tmpText += '                <a class="_32rk _32rg _1cy6  btn_back_quickreply" href="#" style="display: none;">';
                    tmpText += '                    <div direction="forward" class="_10sf _5x5_">';
                    tmpText += '                        <div class="_5x6d">';
                    tmpText += '                            <div class="_3bwv _3bww">';
                    tmpText += '                                <div class="_3bwy">';
                    tmpText += '                                    <div class="_3bwx">';
                    tmpText += '                                        <i class="_3-8w img sp_RQ3p_x3xMG2 sx_c4c7bc" alt=""></i>';
                    tmpText += '                                    </div>';
                    tmpText += '                                </div>';
                    tmpText += '                            </div>';
                    tmpText += '                        </div>';
                    tmpText += '                    </div>';
                    tmpText += '                </a>';
                    tmpText += '                <a class="_32rk _32rh _1cy6  btn_next_quickreply" href="#" style="display: block;">';
                    tmpText += '                    <div direction="forward" class="_10sf _5x5_">';
                    tmpText += '                        <div class="_5x6d">';
                    tmpText += '                            <div class="_3bwv _3bww">';
                    tmpText += '                                <div class="_3bwy">';
                    tmpText += '                                    <div class="_3bwx">';
                    tmpText += '                                        <i class="_3-8w img sp_RQ3p_x3xMG3 sx_dbbd74" alt=""></i>';
                    tmpText += '                                    </div>';
                    tmpText += '                                </div>';
                    tmpText += '                            </div>';
                    tmpText += '                        </div>';
                    tmpText += '                    </div>';
                    tmpText += '                </a>';
                    tmpText += '            </div>';
                }
                return tmpText;
            }
    }
}

function scrollBar() {
    $(".messages").scrollTop($(".messages").prop('scrollHeight'));
}

window.addEventListener('message', function (event) {
    var widthParent = parseInt(event.data);
    //console.log('init')
    if (widthParent <= 425) {
        $("._3-8j").css('margin', '0px 0px 0px');
        $("._6atl").css('height', '100%');
        $("._6atl").css('width', event.data);
        $("._6ati").css('height', '100%');
        $("._6ati").css('width', event.data);
    }

    // Khi click ô chatbox bên ngoài sẽ gửi sự kiến tới khung chat
    if (firstClick == false) {
        new messageBot.renderTemplate('', '').Typing();
        new messageBot.getMessage('postback_card_' + setting.FirstCardID, '', setting.BotID);
        firstClick = true;
    }
}, false);

function resetAudio() {
    $("#btn-remove-audio").addClass('d-none');
    $(".form-input-audio").addClass('d-none');

    $(".form-input-text").removeClass('d-none');
    $(".menu-footer").removeClass('d-none');
    $(".form-action-audio").removeClass('d-none');

    window.clearTimeout(timeOutAudio);
    document.getElementById('audio-realtime').innerHTML = "00:00";
}


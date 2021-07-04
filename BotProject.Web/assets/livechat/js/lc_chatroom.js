var TYPE_USER_CONNECT = {
    CUSTOMER: 'customer',
    AGENT: 'agent',
    BOT: 'bot'
}

var UserStatus = {
    Online: '200',
    Offline: '201'
}

var ApplicationChannel = {
    Web: "0",
    Facebook: "1",
    Zalo: "2",
    Kiosk: "3"
}
var configs = {},
    apiChatRoom = {
        getListCustomerJoin: "api/lc_chatroom/getListCustomerJoinChatChannel",
        getCustomerById: "api/lc_customer/getById",
        getListMessage: "api/lc_message/getByThreadId"
    }

var _channelGroupId = $("#channelGroupId").val(),
    _agentId = $("#userId").val(),
    _agentName = $("#userName").val(),
    _botId = $("#botId").val()

var AgentModel = {
    ID: _agentId,
    Name: _agentName,
    ChannelGroupID: _channelGroupId
}

//var isRenewConversation = false;

var interval_focus_tab_id;

var objHub = $.connection.chatHub;

$(function () {
    cHub.register();
    cHub.receivedSignalFromServer();
    actionChat.init();
});

var cHub = {
    register: function () {
        $.connection.hub.logging = true;
        $.connection.hub.qs = 'isAgentConnected=true';
        $.connection.hub.start({
            transport: ['longPolling', 'webSockets']
        });
        $.connection.hub.start().done(function () {
            console.log("signalr started")
            //objHub.server.connectAgentToListCustomer(_agentId, _channelGroupId);
            objHub.server.connectAgentToChannelChat(_agentId, _channelGroupId)

            new customerEvent.customerJoin().GetListCustomerFromDb();
        });
        $.connection.hub.error(function (error) {
            console.log('SignalR error: ' + error)
        });

        let tryingToReconnect = false;
        $.connection.hub.reconnecting(function () {
            tryingToReconnect = true;
            console.log('SingalR connect đang kết nối lại')
        });

        $.connection.hub.reconnected(function () {
            tryingToReconnect = false;
            console.log('SingalR connect đã kết nối lại')
        });

        $.connection.hub.disconnected(function () {
            console.log('SingalR connect ngắt kết nối')
            if ($.connection.hub.lastError) {
                console.log("Disconnected. Reason: " + $.connection.hub.lastError.message);
                setTimeout(function () {
                    console.log('SingalR connect đang khởi động lại')
                    $.connection.hub.start({
                        transport: ['longPolling', 'webSockets']
                    });
                    $.connection.hub.start().done(function () { });
                }, 5000); // Restart connection after 5 seconds.          
            }

            if (tryingToReconnect) {
                setTimeout(function () {
                    console.log('SingalR connect đang khởi động lại')
                    $.connection.hub.start({
                        transport: ['longPolling', 'webSockets']
                    });
                    $.connection.hub.start().done(function () { });
                }, 5000); // Restart connection after 5 seconds.          
            }
        });
    },
    receivedSignalFromServer: function () {
        objHub.client.receiveNewCustomerToAgent = function (channelGroupId, threadId, objCustomer, conversationId) {
            // Kiểm tra tín hiệu trả về theo đúng kênh chat
            if (channelGroupId == _channelGroupId) {
                // customer mới tham gia
                console.log('customer-' + objCustomer.ID + ' new join')

                new customerEvent.customerJoin().GetCustomerJoinRealtime(threadId, objCustomer);

                objHub.server.agentJoinChatCustomer(threadId);
                objHub.server.sendActionChat(_channelGroupId, threadId, "Hôm nay", AgentModel.ID, objCustomer.ID);
                objHub.server.sendTyping(_channelGroupId, threadId, AgentModel.ID, AgentModel.Name, objCustomer.ID, true, TYPE_USER_CONNECT.AGENT);

                setTimeout(function () {
                    objHub.server.sendMessage(_channelGroupId, threadId, "Xin chào! Bạn có vấn đề gì cần giải đáp ạ?", _agentId, objCustomer.ID, _agentName, TYPE_USER_CONNECT.AGENT);
                }, 500)

                playAudioNotifyMessage();
            }
        };
        objHub.client.getStatusCustomerOffline = function (channelGroupId, customerId) {
            if (channelGroupId == _channelGroupId) {
                console.log('customer-' + customerId + ' offline')
                var $elemCustomer = $("#customer-" + customerId + "");
                $elemCustomer.find('span.avatar').removeClass("avatar-state-online").addClass("avatar-state-offline");
            }
        };
        objHub.client.getStatusCustomerOnline = function (channelGroupId, customerId) {
            if (channelGroupId == _channelGroupId) {
                console.log('customer-' + customerId + ' online')
                var $elemCustomer = $("#customer-" + customerId + "");
                $elemCustomer.find('span.avatar').removeClass("avatar-state-offline").addClass("avatar-state-online");
                isRenewConversation = true;
            }
        };
        objHub.client.receiveMessages = function (channelGroupId, threadId, message, agentId, customerId, userName, typeUser) {
            if (channelGroupId == _channelGroupId) {
                console.log('threadId:' + threadId + '  customer-' + customerId + ' : ' + message)
                if (typeUser == TYPE_USER_CONNECT.CUSTOMER) {
                    insertPreviewChat("customer", customerId, message);
                }
                var $elmCustomer = $("#customer-" + customerId);
                if ($elmCustomer.hasClass("active")) {
                    userName = (userName == "" ? "W" : userName);
                    if (typeUser == TYPE_USER_CONNECT.CUSTOMER) {
                        insertChat(typeUser, customerId, isValidURLandCodeIcon(message), userName, "");
                    }
                    if (typeUser == TYPE_USER_CONNECT.AGENT) {
                        if (_agentId != agentId) {
                            // nếu tài khoản hổ trợ vào được xem lun tin nhắn
                            insertChat(typeUser, customerId, isValidURLandCodeIcon(message), userName, "");
                        }
                    }
                    if (typeUser == TYPE_USER_CONNECT.BOT) {
                        if (message != null && message != "") {
                            let newMessage = message.replace('{bot}', 'me');
                            $("#message-container-" + customerId).append(newMessage);
                            // scroll to bottom
                            setTimeout(function () {
                                $(".messages").getNiceScroll(0).doScrollTop($("#message-container-" + customerId).prop('scrollHeight'));
                            }, 200)
                        }
                    }
                }
                playAudioNotifyMessage();
            }
        };
        objHub.client.receiveTyping = function (channelGroupId, threadId, agentId, agentName, customerId, isTyping, typeUser) {
            if (channelGroupId == _channelGroupId) {
                console.log('customerId-' + customerId + ' typing')
            }
        };

        objHub.client.receiveSignalCustomerFocusTabChat = function (channelGroupId, threadId, customerId, isFocusTab) {
            if (channelGroupId == _channelGroupId) {
                isFocusTab = (isFocusTab == true ? "đã xem" : " k xem");
                console.log('customer-' + customerId + ' ' + isFocusTab);
                var $elmCustomer = $("#customer-" + customerId);
                if (isFocusTab) {
                    if ($elmCustomer.hasClass("active")) {
                        $("#message-container-" + customerId).find('.message-item-action').html("Read");
                    }
                }
            }
        };
    }
}
var customerEvent = {
    getFormChat: function (objCustomer, threadId) {
        var isTyping = false;
        // lấy thông tin thiết bị khách hàng truy cập
        var renderFormDeviceInfo = function () {
            $("#chat-sidebar-device").show();
            $("#chat-sidebar-template-loading").hide();
            if (objCustomer.Devices != null) {
                var device = objCustomer.Devices[0];
                $("#device-city").empty().append(device.City == null ? "" : " " + device.City);
                $("#device-ip").empty().append(device.IPAddress == null ? "" : device.IPAddress);
                $("#device-os").empty().append(device.OS == null ? "" : device.OS);
                $("#device-browser").empty().append(device.Browser == null ? "" : device.Browser);
                $("#device-user-agent").empty().append(device.FullUserAgent == null ? "" : device.FullUserAgent);
                if (device.Latitude != "") {
                    let latinglongTude = device.Latitude + "," + device.Longtitude;
                    $("#LatiLongTude").val(latinglongTude);
                    initLatiLongMap(device.Latitude, device.Longtitude);
                }
            }
            $('.chat-sidebar-content').niceScroll();
        }();

        // lấy danh sách tin nhắn
        var renderFormMessage = function () {
            $(".customer-name").html(objCustomer.Name);
            $(".chat-header").show();
            var htmlChatSetting = '';
            if (_botId != null && _botId != undefined) {
                htmlChatSetting += ' <a class="dropdown-item" href="javascript:void(0);" id="btnTransfer">';
                htmlChatSetting += '                            <i class="fa fa-exchange-alt mr-2"></i>';
                htmlChatSetting += '                             <input type="checkbox" class="mr-2" id="chkTransferToBot-' + objCustomer.ID + '"/>';
                htmlChatSetting += '                             Chuyển tới Bot';
                htmlChatSetting += '                         </a>';
            }

            htmlChatSetting += '                          <a class="dropdown-item" href="javascript:void(0);" id="btnCompleteConversation-' + objCustomer.ID + '">';
            htmlChatSetting +='                                 <i class="fa fa-check mr-2"></i> Hoàn thành cuộc thoại';
            htmlChatSetting +='                          </a>';
            htmlChatSetting += '                         <a class="dropdown-item" href="javascript:void(0);" id="btnBanChat-' + objCustomer.ID + '">';
            htmlChatSetting +='                             <i class="fa fa-ban mr-2"></i> Cấm trò chuyện';
            htmlChatSetting +='                         </a>';
            $("#form-message-setting").empty().append(htmlChatSetting);

            $(".messages").show();

            $('div.messages').attr('id', 'message-container-' + objCustomer.ID + '');

            $(".chat-footer").show();
            var htmlChatFooter = `<div class="flex-grow-1" style="position:relative">
                                        <input type="text" class ="form-control" id= "input-chat-message-` + objCustomer.ID + `" placeholder="Nhập tin nhắn..." data-emojiable="true">
                                    </div>
                                    <div class="chat-footer-buttons d-flex">
                                        <button type="submit" id= "btn-chat-submit-` + objCustomer.ID + `" >
                                            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" class="feather feather-send width-15 height-15"><line x1="22" y1="2" x2="11" y2="13"></line><polygon points="22 2 15 22 11 13 2 9 22 2"></polygon></svg>
                                        </button>
                                   </div>`
            $("#chat-sidebar-message-footer").empty().append(htmlChatFooter)

            $(function () {
                window.emojiPicker = new EmojiPicker({
                    emojiable_selector: '[data-emojiable=true]',
                    //assetsPath: '../asset/emoji-picker/img',
                    assetsPath: _Host + 'assets/client/libs/emoji-picker/img',
                    popupButtonClasses: 'fa fa-smile'
                });
                window.emojiPicker.discover();
            });
        }();


        $("#chkTransferToBot-" + objCustomer.ID).change(function () {
            let isTransfer = false;
            let contentAction = showTimeChat() + ' - Bot tham gia hội thoại';
            if ($(this).is(":checked")) {
                $(this).val('true');
                isTransfer = true;
                contentAction = showTimeChat() + ' - Bot tham gia hội thoại';
            } else {
                $(this).val('false');
                isTransfer = false;
                contentAction = showTimeChat() + ' - Bot thoát hội thoại';
            }
            objHub.server.transferCustomerToBot(_channelGroupId, threadId, AgentModel.ID, _botId, isTransfer);

            objHub.server.sendActionChat(_channelGroupId, threadId, contentAction, AgentModel.ID, objCustomer.ID);

            insertActionChat(objCustomer.ID, contentAction)
        })

        $($($("#input-chat-message-" + objCustomer.ID).next()).eq(0)).focus(function (e) {
                if (!interval_focus_tab_id) {
                    interval_focus_tab_id = setInterval(function () {
                        console.log("agent xem");
                        let isFocusTab = true;
                        objHub.server.checkAgentFocusTabChat(_channelGroupId, threadId, objCustomer.ID, isFocusTab);
                        clearInterval(interval_focus_tab_id);
                    }, 1000);
                }
        })

        $($($("#input-chat-message-" + objCustomer.ID).next()).eq(0)).blur(function (e) {
            clearInterval(interval_focus_tab_id);
            interval_focus_tab_id = 0;
            let isFocusTab = false;
            objHub.server.checkAgentFocusTabChat(_channelGroupId, threadId, objCustomer.ID, isFocusTab);
            console.log("agent k xem")
        })

        $($($("#input-chat-message-" + objCustomer.ID).next()).eq(0)).keyup(function (e) {
            var edValue = $(this);
            var text = edValue.text();
            if (text.length > 0) {
                if (isTyping == false) {
                    isTyping = true;
                    objHub.server.sendTyping(_channelGroupId, threadId, AgentModel.ID, AgentModel.Name, objCustomer.ID, isTyping, TYPE_USER_CONNECT.AGENT);
                }
            } else {
                isTyping = false;
                objHub.server.sendTyping(_channelGroupId, threadId, AgentModel.ID, AgentModel.Name, objCustomer.ID, isTyping, TYPE_USER_CONNECT.AGENT);
            }
        })

        $($($("#input-chat-message-" + objCustomer.ID).next()).eq(0)).keydown(function (e) {
            var edValue = $(this);
            var text = edValue.html();
            if (e.which == 13) {
                e.preventDefault(e);
                if (text !== "") {

                    insertChat("agent", objCustomer.ID, isValidURLandCodeIcon(text), _agentName, "");

                    var indexPosition = $("#div-list-customers a#customer-" + objCustomer.ID).prevAll().length;
                    // đổi vị trí lên top khi focus chat với customer
                    changePositon("#div-list-customers a:eq(" + indexPosition + ")")
                    // gửi tin nhắn
                    objHub.server.sendMessage(_channelGroupId, threadId, text, _agentId, objCustomer.ID, _agentName, TYPE_USER_CONNECT.AGENT);

                    $(this).val('');
                    $(this).text('');
                    isTyping = false;
                    return;
                }
            }
        })

        $("#btn-chat-submit-" + objCustomer.ID).on('click', function () {
            var edValue = $("#input-chat-message-" + objCustomer.ID);
            var text = edValue.val();
            console.log(text)
            if (text !== "") {

                insertChat("agent", objCustomer.ID, isValidURLandCodeIcon(text), _agentName, "");

                // gửi tin nhắn
                objHub.server.sendMessage(_channelGroupId, threadId, text, _agentId, objCustomer.ID, _agentName, TYPE_USER_CONNECT.AGENT);
                $(this).val('');
            }
            return;
        })
    },
    customerJoin: function () {
        this.GetListCustomerFromDb = function () {
            var params = {
                channelGroupId: _channelGroupId
            }
            $.ajax({
                url: _Host + apiChatRoom.getListCustomerJoin,
                contentType: 'application/json; charset=utf-8',
                data: params,
                type: 'GET',
                success: function (data) {
                    if (data.length != 0) {
                        var templateCustomer = '';
                        $.each(data, function (index, value) {
                            let templateHtml = new customerEvent.render(value.ThreadID).templateListCustomer(value);
                            templateCustomer += templateHtml;
                        })
                        $("#div-list-customers").empty().append(templateCustomer);

                        getTimeAgo();

                        // auto focus customer đầu tiên
                        setTimeout(function () {
                            new customerEvent.customerJoin().ViewFormChatById(data[0].ID, data[0].ThreadID)
                        }, 1000)
                    }
                },
            });
        },
        this.GetCustomerJoinRealtime = function (threadID, customer) {
            var html = new customerEvent.render(threadID).templateListCustomer(customer);
            $("#div-list-customers").prepend(html);
            getTimeAgo();
        }
        this.ViewFormChatById = function (customerId, threadId) {
            var $elemCustomer = $("#customer-" + customerId + "");
            if ($elemCustomer.hasClass("active")) {
                return;
            }
            // hightlight active
            $(".list-customer-item").removeClass("active");
            $elemCustomer.addClass("active");
            // gửi tín hiệu kết nối tới nhóm có customer mới
            if ($elemCustomer.find('span.avatar').hasClass("avatar-state-online")) {
                console.log('customer has online')
                objHub.server.agentJoinChatCustomer(threadId);
            }
            var params = {
                customerId: customerId
            }
            $.ajax({
                url: _Host + apiChatRoom.getCustomerById,
                contentType: 'application/json; charset=utf-8',
                data: params,
                type: 'GET',
                success: function (data) {
                    console.log(data)
                    new customerEvent.getFormChat(data, threadId);
                },
            });
        }
    },
    render: function (threadID) {
        this.templateListCustomer = function (customer) {
            var templateHtml = '';
            if (customer.ApplicationChannels == ApplicationChannel.Web) {
                templateHtml += '<a class="list-group-item d-flex list-customer-item" id="customer-' + customer.ID + '" data-customer-id="' + customer.ID + '" data-thread-id="' + threadID + '" href="javascript:new customerEvent.customerJoin().ViewFormChatById(\'' + customer.ID + '\',\'' + threadID + '\')">';
                templateHtml += '<div class="pr-3">';
                templateHtml += '<span class="avatar ' + (customer.StatusChatValue == 200 ? "avatar-state-online" : "avatar-state-offline") + '">';
                templateHtml += '<span class="avatar-title bg-warning rounded-circle">W</span>';
                templateHtml += '</span>';
                templateHtml += '</div>';
                templateHtml += '<div class="flex-grow- 1">';
                templateHtml += '<h6 class="mb-1">' + customer.Name + '</h6>';
                templateHtml += '<span class="small text-muted">';
                templateHtml += '<span id="msg-preview-' + customer.ID + '"></span>';
                templateHtml += '</span>';
                templateHtml += '</div>';
                if (customer.LogoutDate != null) {
                    templateHtml += '<div class="text-right ml-auto">';
                    templateHtml += '<span class="small text-muted timeago" datetime="' + customer.LogoutDate + '"></span>';
                    templateHtml += '</div>';
                } 
                templateHtml += '</a>';
            } else {
                templateHtml += '<a class="list-group-item d-flex list-customer-item" id="customer-' + customer.ID + '" data-customer-id="' + customer.ID + '" data-thread-id="' + threadID + '" href="javascript:void(0)">';
                templateHtml += '<div class="pr-3">';
                templateHtml += '<span class="avatar ' + (customer.StatusChatValue == 200 ? "avatar-state-online" : "avatar-state-offline") + '">';
                templateHtml += '<img src="' + _Host + customer.Avatar + '" class="rounded-circle" alt="image">';
                templateHtml += '</span>';
                templateHtml += '</div>';
                templateHtml += '<div class="flex-grow- 1">';
                templateHtml += '<h6 class="mb-1">';
                templateHtml += '<span>';
                if (customer.ApplicationChannels == ApplicationChannel.Facebook) {
                    templateHtml += '<img class="css-gkenkv" src="~/assets/client/img/fb-msg-icon-960x960.png" alt="channel-icon">';
                } else if (customer.ApplicationChannels == ApplicationChannel.Zalo) {
                    templateHtml += '<img class="css-gkenkv" src="~/assets/client/img/zalo-msg-icon.png" alt="channel-icon">';
                }
                templateHtml += '</span>';
                templateHtml += '</h6>';
                templateHtml += '<span class="small text-muted">';
                templateHtml += '<span id="msg-preview-' + customer.ID + '"></span>';
                templateHtml += '</span>';
                templateHtml += '</div>';
                templateHtml += '<div class="text-right ml-auto">';
                templateHtml += '<span class="small text-muted timeago" datetime="' + customer.CreatedDate + '"></span>';
                templateHtml += '</div>';
                templateHtml += '</a>';
            }
            return templateHtml;
        }
    }
}

var actionChat = {
    init:function(){
        actionChat.Transfer();

    },
    Transfer: function () {
        $("body").on('click', '#btnTransfer', function () {
            $('#modalTransfer').modal('show');
        })
    },
    Ticked: function () {

    },
    BanChat: function () {

    },
    StopChat: function () {

    }
}

var chatMessages = {
    name: "ms1",
    messageText: {
        Content: 'abc',
        Type: 'Text'
    },
    avatar: "",
    showTime: true,
    time: ''
}
var templateTypeMessage = {
    Text: function () {
        var html = '';
        return html;
    },
    TextAndButton: function () {
        var html = '';
        return html;
    },
    Generic: function () {
        var html = '';
        return html;
    },
    Image: function () {
        var html = '';
        return html;
    },
    File: function () {
        var html = '';
        return html;
    },
    Video: function () {
        var html = '';
        return html;
    },
    QuickReply: function () {
        var html = '';
        return html;
    }
}

function insertActionChat(customerId, contentAction) {
    var $elmCustomer = $("#customer-" + customerId);
    if ($elmCustomer.hasClass('active')) {
        var htmlAction = '<div class="message-item message-item-divider">';
        htmlAction +=       '<span>' + contentAction + '</span>';
        htmlAction +='    </div>';
        $("#message-container-" + customerId).append(htmlAction)
        // scroll to bottom
        setTimeout(function () {
            $(".messages").getNiceScroll(0).doScrollTop($("#message-container-" + customerId).prop('scrollHeight'));
        }, 200)
    }
}


function insertChat(who, customerId, text, userName, avatar) {
    let user_class_chat = (who == "agent" ? "me" : "guest");
    let date_current = showTimeChat();
    var contentMessage = '';

    var $elementMessage = document.getElementsByClassName('message-item');
    if ($elementMessage !== undefined || $elementMessage !== null) {
        let $elementLastMessage = $($elementMessage[$elementMessage.length - 1]);
        let timeLastMessage = $elementLastMessage.find('.message-user-time').html();
        let identity_user = $elementLastMessage.attr('data-user');
        if ((identity_user == who) && (date_current == timeLastMessage)) {
            let elementLastMessageAppend = $elementLastMessage.find('.message-item-content').last();
            appendMessage(elementLastMessageAppend, who, customerId, text);
            return;
        }
    }

    contentMessage = '<div class="message-item ' + user_class_chat + '" data-user="' + who + '">';
    contentMessage += message.getUserIcon(who, userName, avatar);
    contentMessage += message.getHtmlMessageBody(who, userName, text, date_current);
    contentMessage += '</div>';

    // insert preview
    insertPreviewChat(who, customerId, text);

    // append message chat
    appendMessage("", who, customerId, contentMessage);
    return;
}
function appendMessage(elementLastMessageAppend, who, customerId, text) {
    if (elementLastMessageAppend !== "") {
        // append body message
        var content = '<div class="message-item-content">' + text + '</div>';
        $(elementLastMessageAppend).after($(content).hide().fadeIn(300));

        // bùa thêm thẻ div trống để active scroll tới bottom trong trường hợp insertAfter k phải elemnt chính nó
        $("#message-container-" + customerId).append("<div></div>");
    } else {
        // append body message
        $("#message-container-" + customerId).append(text).children(':last').hide().fadeIn(300);
    }
    // remove action read, delivered, message not send
    if (who == TYPE_USER_CONNECT.CUSTOMER) {
        $("#message-container-" + customerId).find('.message-item-action').remove();
    }
    // scroll to bottom
    setTimeout(function () {
        $(".messages").getNiceScroll(0).doScrollTop($("#message-container-" + customerId).prop('scrollHeight'));
    }, 200)
}


function insertEventChat(customerId, time) {
    var html = '';
    html += '<div class="message-item message-item-divider">';
    html += '                        <span>Hôm nay</span>';
    html += '                    </div>';
    $("#message-container-" + customerId).append(html);
    isRenewConversation = false;
    return;
}

function insertPreviewChat(who, customerId, text) {
    var user = (who == "agent" ? "Bạn" : "Khách");
    var msg_previve = '';
    if (text.length > 20) {
        text = text.substring(0, 19) + "...";
    }
    msg_previve = user + ": " + text;
    $("#msg-preview-" + customerId).empty().append(msg_previve);
}

var message = {
    getUserIcon: function (who, userName, avatar) {
        var firstNameCharacter = userName.substring(0, 1).toUpperCase();
        var templateAvatar = '';
        templateAvatar += '<div class="message-avatar">';
        templateAvatar += '<div class="message-avatar-customer">';
        templateAvatar += '<div class="pr-3">';
        templateAvatar += '<span class ="message-avatar-item avatar">';
        if (avatar == "") {
            templateAvatar += '<span class ="avatar-title ' + (who == "agent" ? "bg-primary" : "bg-warning") + ' rounded-circle">' + firstNameCharacter + '</span>';
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
        if (who == "agent") {
            templateMsg += '<div class="message-item-action txt-align-left font-size-08">Delivered</div>';//Message not sent, , Read
        }
        templateMsg += '</div>';
        return templateMsg;
    }
}

// Đổi vị trí user lên top khi chat
function changePositon(selector) {
    var elementSelect = $(selector);
    // Lấy vị trí top,lef của thẻ element tag li lawyer đầu tiên
    var posLiFirst = $("#div-list-customers a:eq(0)").offset();
    // Lấy vị trí top,lef của thẻ element tag li lawyer khi chọn click
    var oldPosLiSelect = $(selector).offset();
    if (posLiFirst.top == oldPosLiSelect.top)
        return;

    // Sao chép element li select thành vị trí đầu tiên
    var newLiFirst = elementSelect.clone().insertBefore('#div-list-customers a:eq(0)');
    newLiFirst.hide();

    // Tạo thẻ li ảo di chuyển
    var temp = elementSelect.clone().appendTo('body')
    temp.css('left', oldPosLiSelect.left)
        .css('top', oldPosLiSelect.top)
        .css('width', $('#div-list-customers a:eq(0)').offsetWidth)
        .css('height', $('#div-list-customers a:eq(0)').offsetHeight)
    temp.addClass('temp');

    //elementSelect.hide();
    temp.animate({
        'top': (posLiFirst.top + 30),
        'left': posLiFirst.left
    }, 0, function () { // thẻ ảo di chuyển
        temp.remove(); // di chuyen xong remove
        elementSelect.remove();
        newLiFirst.show()
    });
    $(".chat-sidebar-content").getNiceScroll(0).doScrollTop(0);
    return;
};



var agentTable = {
    init: function () {
        agentTable.getListAgents();
        agentTable.getListBotByUser();
    },
    getListBotByUser: function () {
        var param = {
            userID: userModel.UserID
        }
        $.ajax({
            url: _Host + api.getListBotByUserId,
            contentType: 'application/json; charset=utf-8',
            data: param,
            type: 'GET',
            success: function (result) {
                console.log(result)
                if (result.length != 0) {
                    new agentTable.renderTempChatbot(result);
                }
            },
        });
    },
    getListAgents: function () {
        var param = {
            channelGroupId: userModel.channelGroupId
        }
        param = JSON.stringify(param)
        $.ajax({
            url: _Host + api.getListAgentChannel,
            contentType: 'application/json; charset=utf-8',
            data: param,
            type: 'POST',
            success: function (result) {
                console.log(result)
                if (result.length != 0) {
                    new agentTable.renderTempAgent(result);
                    new agentTable.renderTempGroup(result);
                }

            },
        });
    },
    renderTempAgent: function (data) {
        var html = '';
        $.each(data, function (index, value) {
            html += '<tr>';
            html += ' <td>';
            html += '      <div size="2" data-test="user-avatar" class="css-1ocrak0 css-7e05130"><div class="css-5r5m5i css-7e05132"></div><span class="css-10zmg9r css-7e05131"></span></div>';
            html += '  </td>';
            html += '  <td>' + value.Email + '</td>';
            html += '  <td>';
            html += '      <span class="css-y4ek3x" title="Desktop"><div class="css-nmb5ix css-j314r80" width="20px" height="20px"><svg width="20px" height="20px" viewBox="0 0 20 18"><g fill="none" fill-rule="evenodd" opacity=".7"><path d="M0-1h20v20H0z"></path><path fill="#000" fill-rule="nonzero" d="M17.5.667h-15c-.917 0-1.667.75-1.667 1.666v10C.833 13.25 1.583 14 2.5 14h5.833v1.667H6.667v1.666h6.666v-1.666h-1.666V14H17.5c.917 0 1.667-.75 1.667-1.667v-10c0-.916-.75-1.666-1.667-1.666zm0 11.666h-15v-10h15v10z"></path></g></svg></div></span>';
            html += '  </td>';
            html += '  <td>';
            html += '      <span class="badge bg-success-bright text-success">' + value.ApplicationGroupName + '</span>';
            html += '  </td>';
            html += '  <td></td>';
            html += '</tr>';
        })
        $("#tbl-lst-agent").append(html);
        $("#tab-agents-count").html(' (' + data.length + ')');
    },
    renderTempChatbot: function (data) {
        var html = '';
        $.each(data, function (index, value) {
            html += '<tr>';
            html += '<td>';
            html += '<div size="2" data-test="user-avatar" class="css-1ocrak0 css-7e05130">';
            html += '	<div class="css-5r5m5i css-7e05132"></div>';
            html += '	<span class="css-10zmg9r css-7e05131"></span>';
            html += '</div>';
            html += '</td>';
            html += '<td>' + value.Name + '</td>';
            html += '<td>';
            html += '	<span class="badge bg-success-bright text-success">Chatbot</span>';
            html += '</td>';
            if (value.IsActiveLiveChat) {
                html += '<td>';
                html += '	<span class="badge bg-success-bright text-success">ON</span>';
                html += '</td>';
            } else {
                html += '<td>';
                html += '	<span class="badge bg-warning text-dark">OFF</span>';
                html += '</td>';
            }
            html += '<td>';
            html += '	<span class="badge bg-info-bright text-dark"></span>';
            html += '</td>';
            html += '</tr>';
        })

        $("#tbl-lst-chatbot").append(html);
        $("#tab-chatbot-count").html(' (' + data.length + ')');

    }
};
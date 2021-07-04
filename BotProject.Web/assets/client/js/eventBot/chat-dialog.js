//---------------------------- CHATBOX EVENT ----------------------------//
/*
    
*/
var configs = {
    color: $("#botColor").val(),
    srcLogo: _Host + $("#botLogo").val(),
    isActiveGetStarted: false,
    isActiveAccentVN: JSON.parse(localStorage.getItem("cbot_chk_accent")),
    isActiveViewDetailPopup: JSON.parse(localStorage.getItem("cbot_chk_popup"))
},
    api = {
        getAccentVN: "accentVN/convertVN",
        getMessageBot: "apiv1/chatbot"
    },
    message_nf = {
        T_01: "Xin lỗi, Tôi không hiểu",
        T_02: "Bạn có thể giải thích thêm được không?",
        T_03: "Tôi không thể tìm thấy, bạn có thể nói rõ hơn?",
        T_04: "Xin lỗi, Bạn có thể giải thích thêm được không?",
        T_05: "Rất tiếc! Tôi chưa được học để hiểu câu hỏi này",
        T_06: "Tôi chưa hiểu ạ, bạn nói rõ hơn được không?"
    }
$(function () {
    cboxEvent.init();
    // Chạy ngầm trước để load thư viện có dấu
    //setTimeout(function () {
    //    msgEvent.getMessageAccentVN('abc');
    //}, 1500)
});
var cboxEvent = {
    init: function () {
        this.events();
        this.setting();
    },
    setting: function () {
        configs.isActiveAccentVN = true;
        $('#chk-stt-accent').prop('checked', configs.isActiveAccentVN);
        $("#chk-stt-popup").prop('checked', configs.isActiveViewDetailPopup);
    },
    events: function () {
        $("body").on('click', '.gl_next_carousel', function () {
            var $form = $(this).closest('.form_carousel');
            var currentIndex = $form.find($('div._a28')).attr('index');
            var newIndex = (parseInt(currentIndex) + 1);
            var maxIndex = $form.find($('div._2zgz')).length - 1;
            $form.find($('div._a28')).attr('index', newIndex);
            var calPX = -272 * newIndex;
            var leftPX = '' + calPX + 'px';
            $form.find('._a2e').css('left', leftPX);
            // show back
            $form.find('.gl_back_carousel').css('display', 'block');
            if (newIndex == maxIndex) {
                $form.find('.gl_next_carousel').css('display', 'none');
            }
        })
        $("body").on('click', '.gl_back_carousel', function () {
            var $form = $(this).closest('.form_carousel');
            var currentIndex = $form.find($('div._a28')).attr('index');
            var newIndex = (parseInt(currentIndex) - 1);
            $form.find($('div._a28')).attr('index', newIndex);

            var calPX = -272 * newIndex;
            var leftPX = '' + calPX + 'px';

            $form.find('._a2e').css('left', leftPX);
            if (newIndex == 0) {
                $form.find('.gl_back_carousel').css('display', 'none');
            }
            $form.find('.gl_next_carousel').css('display', 'block');
        })
        $("body").on('click', '.btn_next_carousel', function () {
            var $form = $(this).closest('.form_carousel');
            var currentIndex = $form.find($('div._a28')).attr('index');
            var newIndex = (parseInt(currentIndex) + 1);
            var maxIndex = $form.find($('div._2zgz')).length - 1;
            var calPX = -155 * newIndex;
            var leftPX = '' + calPX + 'px';

            $form.find($('div._a28')).attr('index', newIndex);
            $form.find('._a2e').css('left', leftPX);

            // show back
            $form.find('.btn_back_carousel').css('display', 'block');
            if (newIndex == maxIndex) {
                $form.find('.btn_next_carousel').css('display', 'none');
            }
        })
        $("body").on('click', '.btn_back_carousel', function () {
            var $form = $(this).closest('.form_carousel');
            var currentIndex = $form.find($('div._a28')).attr('index');
            var newIndex = (parseInt(currentIndex) - 1);
            $form.find($('div._a28')).attr('index', newIndex);

            var calPX = -155 * newIndex;
            var leftPX = '' + calPX + 'px';

            $form.find('._a2e').css('left', leftPX);
            if (newIndex == 0) {
                $form.find('.btn_back_carousel').css('display', 'none');
            }
            $form.find('.btn_next_carousel').css('display', 'block');

        })
        $('body').on('click', '._661n_btn_menu_chat', function () {
            $('.uiContextualLayerPositioner').toggle();
        })
        $('body').on('click', '._1qd3_btn_setting', function () {
            $('._4fsi').toggle();
        })
        // close form
        $('body').on('click', '._1qd1_close_form', function (e) {
            parent.postMessage("close", "*");
        })
        // INPUT TEXT
        $('#58al-input-text').keydown(function (e) {
            var text = $(this).val();
            $("._4bqf_btn_submit").show();
            if (e.which == 13) {
                e.preventDefault(e);
                if (text.trim() !== "") {
                    $("._4bqf_btn_submit").hide();
                    $(this).val('');
                    msgEvent.sendMessage(text, '');
                }
            }
        })
        $('body').on('click', '._4bqf_btn_submit', function (e) {
            var text = $("#58al-input-text").val();
            if (text.trim() !== "") {
                msgEvent.sendMessage(text, '');
            }
            $('#58al-input-text').val('');
        })
        $('body').click(function (e) {
            if (!$(e.target).closest('._4xko').length) {
                $('.datebreak').removeClass('hide').addClass('hide');
                $('.viewed').removeClass('hide').addClass('hide');
            } else {
                $('.datebreak').removeClass('hide').addClass('hide');
                $('.viewed').removeClass('hide').addClass('hide');
                if ($(e.target).closest('._4xko').parent().prev().hasClass('hide')) {
                    $(e.target).closest('._4xko').parent().prev().removeClass('hide');
                    $(e.target).closest('._4xko').parent().next().removeClass('hide');
                }
            }
        });

        // postback
        $('body').on('click', '._2zgz_postback', function (e) {
            var dataText = $(this).children().eq(0).text();
            var dataPostback = $(this).children().eq(0).attr('data-postback');
            msgEvent.sendMessage(dataText, dataPostback);
            e.stopPropagation();
        })
        //menu
        $('body').on('click', '._6ir4_menu', function (e) {
            e.preventDefault();
            var dataText = $(this).text();
            var dataPostback = $(this).attr('data-postback');
            msgEvent.sendMessage(dataText, dataPostback);
            e.stopPropagation();
        })
		// image - show popup outside iframe
        $('body').on('click', '._6popup_image', function (e) {
            e.preventDefault();
            var elm = $(this);
            var imageURL = $(this).css('background-image');
            imageURL = imageURL.replace(/(url\(|\)|")/g, '');
            parent.postMessage(imageURL, "*");
            //window.open(imageURL, '_blank');
            e.stopPropagation();
        })
        //module
        $('body').on('click', '._6ir4_module', function (e) {
            e.preventDefault();
            var dataText = $(this).text();
            var dataPostback = $(this).attr('data-postback');
            var mdInfoPatientID = $(this).attr('data-id');
            $('.chk-opt-module-' + mdInfoPatientID + '').each(function () {
                if ($(this).prop('checked')) {
                    //console.log($(this).val());
                }
            })
            msgEvent.sendMessage(dataText, dataPostback);
            e.stopPropagation();
        })
        //popup
        $('body').on('click', '._6ir4_popup', function (e) {
            e.preventDefault();
            var urlPopup = $(this).attr('href');
            //show 1 popup ngoài iframe
            //var quesID = $(this).attr('data-id');
            //var domain = 'http://qa.surelrn.vn';//http://localhost:54160;
            //parent.postMessage(quesID, domain);
            parent.postMessage(urlPopup, "*");
            e.stopPropagation();
        })

        // Bùa nhớ xóa
        $('body').on('click', '._4fsj-menu1', function (e) {
            e.preventDefault();
            var dataText = $(this).text();
            var dataPostback = $(this).attr('data-postback');
            msgEvent.sendMessage(dataText, dataPostback);
            // chặn ảnh hưởng tới thẻ a href next
            e.stopPropagation();
        })

        // event click option cbox
        $("#chk-stt-accent").click(function () {
            localStorage.setItem("cbot_chk_accent", $(this).prop('checked'));
        });
        $("#chk-stt-popup").click(function () {
            localStorage.setItem("cbot_chk_popup", $(this).prop('checked'));
        });
    }
}
var msgEvent = {
    sendMessage: function (text, textPostback) {
        //append message user
        msgEvent.renderUser(text);
        //append typing
        msgEvent.typing();

        //get msg bot
        if (textPostback != "") {
            var titlePostback = text;
            msgEvent.getMessageBot(textPostback, titlePostback);
        }
        else {
            if ($("#chk-stt-accent").prop('checked') == true) {

                var textConvertAccentVN = msgEvent.getMessageAccentVN(text);
                if (textConvertAccentVN != "") {
                    text = textConvertAccentVN;
                    var msgConvertAccentVN = new msgEvent.renderBot().TemplateTextConvertAccentVN(text);// tempDidYouMeanBot(response);
                    msgEvent.appendMessage(".conversationContainer", 1000, msgConvertAccentVN, true)
                }
            }
            msgEvent.getMessageBot(text);
        }
        //scroll bottom
        scrollBar();
    },
    appendMessage: function (element, timeout, text, isSendTyping) {
        if (!isSendTyping) {
            isSendTyping = false;
        }
        $(element).delay(timeout).queue(function (next) {
            $("._4xkn_writing").remove();
            $(this).append(text);
            //console.log(isSendTyping)
            if (isSendTyping) msgEvent.typing();
            scrollBar();
            next();
        });
    },
    getMessageAccentVN: function (text) {
        var rs = "";
        $.ajax({
            url: "http://bot.lawcoviet.vn/" + api.getAccentVN + '?text=' + text,//"https://bot.digipro.vn/"
            contentType: 'application/json; charset=utf-8',
            type: 'GET',
            async: false,
            global: false,
            dataType: "json",
        }).done(function (response) {
            console.log(response)
            if (response == "ERROR_400") {
                //console.log("Load data accent vietnamese not success")
                return rs;
            }
            if (response != "") {
                response = response.replace('\n', '.');
                if (text.toLowerCase() != response) {
                    rs = response;
                }
            }
        })
        return rs;
    },
    getMessageStarted: function () {
        if ($("#haveIntroduct").val() == "True") {
            if (configs.isActiveGetStarted == false) {
                // typing
                msgEvent.typing();
                //start
                var content = '';
                if ($("#botTextIntro").val() != '') {
                    content = $("#botTextIntro").val();
                }
                if ($("#cardID").val() != null || $("#cardID").val() != '') {
                    content = "postback_card_" + $("#cardID").val();
                }
                setTimeout(function () {
                    msgEvent.getMessageBot(content);
                }, 500)
                configs.isActiveGetStarted = true;
            }
        }
    },
    getMessageBot: function (text, titlePostback) {
        var param = {
            text: text,
            token: $('#userID').val(),
            botId: $('#botID').val(),
            titlePostback: titlePostback
        }
        param = JSON.stringify(param)
        $.ajax({
            url: _Host + api.getMessageBot,
            contentType: 'application/json; charset=utf-8',
            data: param,
            type: 'POST',
            success: function (result) {
                //console.log(result)
                var isHaveSymptomsAndNotMatch = false;
                var lstMessage = result.message,
                    tplPostback = result.postback[0],
                    resultAPI = result.messageLstSearchNLP,
                    resultLstMedSymptoms = result.messageLstSymptoms,
                    isSearchNLP = result.isSearchNLP;
                if (result.isHaveSymptomsAndNotMatch) {
                    isHaveSymptomsAndNotMatch = result.isHaveSymptomsAndNotMatch || false;
                }
                if (isHaveSymptomsAndNotMatch == true && resultLstMedSymptoms != null && resultLstMedSymptoms.length != 0) {
                    var tempCarouselMedSymptoms = new msgEvent.renderBot().TemplateTextCarouselMedSymptoms(resultLstMedSymptoms);
                    msgEvent.appendMessage(".conversationContainer", 500, new msgEvent.renderBot().TemplateText('Bạn vui lòng xem thêm thông tin triệu chứng bên dưới'), true)
                    msgEvent.appendMessage(".conversationContainer", 1000, tempCarouselMedSymptoms)
                    setTimeout(function () {
                        if (tplPostback !== null) {
                            tplPostback = tplPostback.replace(/{{color}}/g, configs.color);
                            $("#_12cd_event_button").empty().append(tplPostback);
                            scrollBar();
                        }
                    }, 1000)
                }
                if (resultAPI != null && resultAPI.length != 0 && isSearchNLP == true) {
                    var tempCarouselQnA = new msgEvent.renderBot().TemplateTextCarouselQnA(resultAPI);
                    msgEvent.appendMessage(".conversationContainer", 500, new msgEvent.renderBot().TemplateText('Tôi tìm thấy ' + resultAPI.length + ' câu hỏi liên quan đến câu hỏi của bạn.'), true)
                    msgEvent.appendMessage(".conversationContainer", 1000, tempCarouselQnA)
                    setTimeout(function () {
                        if (tplPostback !== null) {
                            tplPostback = tplPostback.replace(/{{color}}/g, configs.color);
                            $("#_12cd_event_button").empty().append(tplPostback);
                            scrollBar();
                        }
                    }, 1000)
                    return;
                }
                if (isHaveSymptomsAndNotMatch == false) {
                    if (lstMessage.length == 1) {
                        let msg = lstMessage[0];
                        switch (msg) {
                            case "NOT_MATCH_01":
                                msg = new msgEvent.renderBot().TemplateText(message_nf.T_01);
                                break;
                            case "NOT_MATCH_02":
                                msg = new msgEvent.renderBot().TemplateText(message_nf.T_02);
                                break;
                            case "NOT_MATCH_03":
                                msg = new msgEvent.renderBot().TemplateText(message_nf.T_03);
                                break;
                            case "NOT_MATCH_04":
                                msg = new msgEvent.renderBot().TemplateText(message_nf.T_04);
                                break;
                            case "NOT_MATCH_05":
                                msg = new msgEvent.renderBot().TemplateText(message_nf.T_05);
                                break;
                            case "NOT_MATCH_06":
                                msg = new msgEvent.renderBot().TemplateText(message_nf.T_06);
                                break;
                        }

                        msg = msg.replace(/{{color}}/g, configs.color);
                        msg = msg.replace(/{{image_logo}}/g, configs.srcLogo);
                        msgEvent.appendMessage(".conversationContainer", 500, msg)
                        if (tplPostback !== null) {
                            tplPostback = tplPostback.replace(/{{color}}/g, configs.color);
                            setTimeout(function () {
                                $("#_12cd_event_button").empty().append(tplPostback);
                                scrollBar();
                            }, 500)
                        }
                    }
                    if (lstMessage.length > 1) {
                        $.each(lstMessage, function (index, value) {
                            index = index + 1;
                            let isSendTyping = true;
                            let msg = value;
                            msg = msg.replace(/{{color}}/g, configs.color);
                            msg = msg.replace(/{{image_logo}}/g, configs.srcLogo);
                            if (index == lstMessage.length) {
                                isSendTyping = false;
                            }
                            msgEvent.appendMessage(".conversationContainer", 500, msg, isSendTyping)
                        })
                        var newTimeDelay = parseInt(lstMessage.length * 500);
                        setTimeout(function () {
                            $("._4xkn_writing").remove();
                            if (tplPostback !== null) {
                                tplPostback = tplPostback.replace(/{{color}}/g, configs.color);
                                $("#_12cd_event_button").empty().append(tplPostback);
                                scrollBar();
                            }
                        }, newTimeDelay)
                    }

                    if (resultLstMedSymptoms != null && resultLstMedSymptoms.length != 0) {
                        var tempCarouselMedSymptoms = new msgEvent.renderBot().TemplateTextCarouselMedSymptoms(resultLstMedSymptoms);
                        msgEvent.appendMessage(".conversationContainer", 500, new msgEvent.renderBot().TemplateText('Bạn vui lòng xem thêm thông tin triệu chứng bên dưới'), true)
                        msgEvent.appendMessage(".conversationContainer", 1000, tempCarouselMedSymptoms)
                        setTimeout(function () {
                            if (tplPostback !== null) {
                                tplPostback = tplPostback.replace(/{{color}}/g, configs.color);
                                $("#_12cd_event_button").empty().append(tplPostback);
                                scrollBar();
                            }
                        }, 1000)
                    }
                }
                scrollBar();
            }
        });
    },
    typing: function () {
        $("#_12cd_event_button").empty();
        var html = '<div class="_4xkn _4xkn_writing clearfix">' +
        '               <div class="profilePictureColumn" style="bottom:0px;">' +
        '                    <div class="_4cqr">' +
        '                         <img class="profilePicture img" src="' + configs.srcLogo + '" alt="">' +
        '                         <div class="clearfix"></div>' +
        '                     </div>' +
        '                </div>' +
        '                <div class="messages">' +
        '                     <div class="_4xko _13y8">' +
        '                          <div class="_4a0v _1x3z">' +
        '                               <div class="_4b0g">' +
        '                                    <div class="_5pd7"></div>' +
        '                                    <div class="_5pd7"></div>' +
        '                                    <div class="_5pd7"></div>' +
        '                               </div>' +
        '                           </div>' +
        '                      </div>' +
        '                </div>' +
        '          </div>';
        //return html;
        $(".conversationContainer").append(html);
    },
    renderBot: function () {
        this.TemplateText = function (text) {
            var htmlText = '<div class="_4xkn clearfix">' +
                '               <div class="profilePictureColumn" style="bottom:0px;">' +
                '                    <div class="_4cqr">' +
                '                         <img class="profilePicture img" src="' + configs.srcLogo + '" alt="">' +
                '                         <div class="clearfix"></div>' +
                '                     </div>' +
                '                </div>' +
                '                <div class="messages">' +
                                  '<div class="_21c3">' +
                '                          <div class="clearfix _2a0-">' +
                '                               <div class="_4xko _4xkr _tmpB" tabindex="0" role="button" style="background-color: rgb(241, 240, 240); font-family: Segoe UI Light;">' +
                '                                   <span>' +
                '                                       <span>' + text + '</span>' +
                '                                   </span>' +
                '                               </div>' +
                '                          </div>' +
                '                     </div>' +
                '                </div>' +
                '          </div>';
            return htmlText;
        },
        this.TemplateTextConvertAccentVN = function (text) {
            var htmlText = '<div class="_4xkn clearfix">' +
                '               <div class="profilePictureColumn" style="bottom:0px;">' +
                '                    <div class="_4cqr">' +
                '                         <img class="profilePicture img" src="' + configs.srcLogo + '" alt="">' +
                '                         <div class="clearfix"></div>' +
                '                     </div>' +
                '                </div>' +
                '                <div class="messages">' +
                                  '<div class="_21c3">' +
                '                          <div class="clearfix _2a0-">' +
                '                               <div class="_4xko _4xkr _tmpB" tabindex="0" role="button" style="font-style:italic;font-family: Segoe UI Light">' +
                '                                   <span>' +
                '                                       <span> Ý bạn là: ' + text + '</span>' +
                '                                   </span>' +
                '                               </div>' +
                '                          </div>' +
                '                     </div>' +
                '                </div>' +
                '          </div>';
            return htmlText;
        },
        this.TemplateTextCarouselQnA = function (lstData) {
            var tempModuleHtml = '';
            var itemHtml = '';
            $.each(lstData, function (index, value) {
                itemHtml += '<div class="_2zgz">';
                itemHtml += '<div class="_6j2h">';
                itemHtml += '<div class="_6j2i">';
                itemHtml += '<div class="_6j2g">';
                itemHtml += '<style>_4ik5._4ik4:nth-child(1)::after{content:""}</style>'
                itemHtml += '<div class="_6j0t _4ik4 _4ik5" style="-webkit-line-clamp: 3;visibility: unset !important;">' + (value.question != null ? add3Dots(value.question, 120) : "") + '</div>';
                itemHtml += '<div class="_6j0v">';
                itemHtml += '<div class="_6j0u _6j0w">' + (value.field != null ? "" : "") + '</div>';
                itemHtml += '<div class="_6j0u _6j0x _4ik4 _4ik5" style="-webkit-line-clamp: 2;">';
                itemHtml += '<div>' + (value.field != null ? "" : "") + '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '<div class="_6ir5">';
                itemHtml += '<div class="_4bqf _6ir3">';
                itemHtml += '<a class="_6ir4 _6ir4_popup qa_law" data-id="' + value.id + '" href="https://bot.surelrn.vn/home/faq?id=' + value.id + '" target="_blank"  rel="nofollow noopener" data-lynx-mode="hover" style="color: rgb(234, 82, 105);">Xem chi tiết</a>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
            })
            tempModuleHtml += ' <div class="_4xkn clearfix">';
            tempModuleHtml += '<div class="profilePictureColumn" style="bottom: 0px;">';
            tempModuleHtml += '<div class="_4cqr">';
            tempModuleHtml += '<img class="profilePicture img" src="' + configs.srcLogo + '" alt="">';
            tempModuleHtml += '<div class="clearfix"></div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '<div class="messages">';
            tempModuleHtml += '<div class="_21c3">';
            tempModuleHtml += '<div class="clearfix _2a0-">';
            tempModuleHtml += '<div class="_4xko _2k7w _4xkr bot_reply">';
            tempModuleHtml += '<div class="">';
            tempModuleHtml += '<div currentselectedindex="0" maxchangeamount="1" class="_23n- form_carousel">';
            tempModuleHtml += '<div class="_4u-c">';
            tempModuleHtml += '<div index="0" class="_a28">';
            tempModuleHtml += '<div class="_a2e">';

            tempModuleHtml += itemHtml;

            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '<div class="_4u-f">';
            tempModuleHtml += '<iframe aria-hidden="true" class="_1_xb" tabindex="-1"></iframe>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            if (lstData.length > 1) {
                tempModuleHtml += '<a class="_32rk _32rg _1cy6 gl_back_carousel" href="#" style="display:none;">';
                tempModuleHtml += '<div direction="backward" class="_10sf _5x5- _5x60">';
                tempModuleHtml += '<div class="_5x6d">';
                tempModuleHtml += '<div class="_3bwv _3bww">';
                tempModuleHtml += '<div class="_3bwy">';
                tempModuleHtml += '<div class="_3bwx"><i class="_3-8w img sp_bfeq6p sx_c4c7bc" alt=""></i></div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</a>';
                tempModuleHtml += '<a class="_32rk _32rh _1cy6 gl_next_carousel" href="#">';
                tempModuleHtml += '<div direction="forward" class="_10sf _5x5_">';
                tempModuleHtml += '<div class="_5x6d">';
                tempModuleHtml += '<div class="_3bwv _3bww">';
                tempModuleHtml += '<div class="_3bwy">';
                tempModuleHtml += '<div class="_3bwx">';
                tempModuleHtml += '<i class="_3-8w img sp_RQ3p_x3xMG3 sx_dbbd74" alt=""></i>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</a>';
            }
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            return tempModuleHtml;
        },
        this.TemplateTextCarouselMedSymptoms = function (lstData) {
            var tempModuleHtml = '';
            var itemHtml = '';
            $.each(lstData, function (index, value) {
                itemHtml += '<div class="_2zgz">';
                itemHtml += '<div class="_6j2h">';
                itemHtml += '<div class="_6j2i">';
                itemHtml += '<div class="_6j2g">';
                itemHtml += '<style>_4ik5._4ik4:nth-child(1)::after{content:""}</style>'
                itemHtml += '<div class="_6j0t _4ik4 _4ik5" style="-webkit-line-clamp: 3;visibility: unset !important">' + (value.description != null ? add3Dots(value.description, 120) : "") + '</div>';
                itemHtml += '<div class="_6j0v">';
                itemHtml += '<div class="_6j0u _6j0w">' + (value.name != null ? "" : "") + '</div>';
                itemHtml += '<div class="_6j0u _6j0x _4ik4 _4ik5" style="-webkit-line-clamp: 2;">';
                itemHtml += '<div>' + (value.name != null ? "" : "") + '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '<div class="_6ir5">';
                itemHtml += '<div class="_4bqf _6ir3">';
                itemHtml += '<a class="_6ir4 _6ir4_popup qa_law" data-id="' + value.id + '" href="https://bot.surelrn.vn/home/FaqMedSymptoms?id=' + value.id + '" target="_blank"  rel="nofollow noopener" data-lynx-mode="hover" style="color: rgb(234, 82, 105);">Xem chi tiết</a>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
                itemHtml += '</div>';
            })
            tempModuleHtml += ' <div class="_4xkn clearfix">';
            tempModuleHtml += '<div class="profilePictureColumn" style="bottom: 0px;">';
            tempModuleHtml += '<div class="_4cqr">';
            tempModuleHtml += '<img class="profilePicture img" src="' + configs.srcLogo + '" alt="">';
            tempModuleHtml += '<div class="clearfix"></div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '<div class="messages">';
            tempModuleHtml += '<div class="_21c3">';
            tempModuleHtml += '<div class="clearfix _2a0-">';
            tempModuleHtml += '<div class="_4xko _2k7w _4xkr bot_reply">';
            tempModuleHtml += '<div class="">';
            tempModuleHtml += '<div currentselectedindex="0" maxchangeamount="1" class="_23n- form_carousel">';
            tempModuleHtml += '<div class="_4u-c">';
            tempModuleHtml += '<div index="0" class="_a28">';
            tempModuleHtml += '<div class="_a2e">';

            tempModuleHtml += itemHtml;

            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '<div class="_4u-f">';
            tempModuleHtml += '<iframe aria-hidden="true" class="_1_xb" tabindex="-1"></iframe>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            if (lstData.length > 1) {
                tempModuleHtml += '<a class="_32rk _32rg _1cy6 gl_back_carousel" href="#" style="display:none;">';
                tempModuleHtml += '<div direction="backward" class="_10sf _5x5- _5x60">';
                tempModuleHtml += '<div class="_5x6d">';
                tempModuleHtml += '<div class="_3bwv _3bww">';
                tempModuleHtml += '<div class="_3bwy">';
                tempModuleHtml += '<div class="_3bwx"><i class="_3-8w img sp_bfeq6p sx_c4c7bc" alt=""></i></div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</a>';
                tempModuleHtml += '<a class="_32rk _32rh _1cy6 gl_next_carousel" href="#">';
                tempModuleHtml += '<div direction="forward" class="_10sf _5x5_">';
                tempModuleHtml += '<div class="_5x6d">';
                tempModuleHtml += '<div class="_3bwv _3bww">';
                tempModuleHtml += '<div class="_3bwy">';
                tempModuleHtml += '<div class="_3bwx">';
                tempModuleHtml += '<i class="_3-8w img sp_RQ3p_x3xMG3 sx_dbbd74" alt=""></i>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</div>';
                tempModuleHtml += '</a>';
            }
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            tempModuleHtml += '</div>';
            return tempModuleHtml;
        }
    },
    renderUser: function (text) {
        var html = '<div class="_4xkn clearfix">' +
        '<div class="messages">' +
        '    <div class="_21c3">' +
        '        <h4 class="datebreak _497p _2lpt hide"><time class="_3oh-">T6 16:52</time></h4>' +
        '        <div class="clearfix _2a0-">' +
        '            <div class="_4xko _4xks" tabindex="0" role="button" style="background-color: ' + configs.color + 'font-family: Segoe UI Light;">' +
        '                 <span>' +
        '                      <span>' +
        '                          ' + text + '' +
        '                      </span>' +
        '                 </span>' +
        '             </div>' +
        '             <a class="_6934 noDisplay" href="#">' +
        '                 This message didn\'t send. Click to try again.' +
        '                 <span class="_21c6 error" title="Đã chuyển"></span>' +
        '             </a>' +
        '        </div>' +
                 '<span class="viewed hide" style="animation: fadeIn 0.1s cubic-bezier(0.4, 0, 0.2, 1) 0s 1 normal both running; clear: both; color: rgba(0, 0, 0, 0.4); float: right; font-size: 12px; font-weight: 500; padding-left: 0px; padding-right: 7px;">Đã xem</span>';
        '     </div>' +
        '</div>' +
    '</div>';
        $(".conversationContainer").append(html);
        //return html;
    }
}


function scrollBar() {
    $(".uiScrollableAreaWrap").scrollTop($(".uiScrollableAreaWrap").prop('scrollHeight'));
}

function add3Dots(string, limit) {
    var dots = "...";
    if (string.length > limit) {
        string = string.substring(0, limit) + dots;
    }
    return string;
}

window.addEventListener('message', function (event) {
    var widthParent = parseInt(event.data);
    //console.log(event.data)
    if (widthParent <= 425) {
        $("._3-8j").css('margin', '0px 0px 0px');
        $("._6atl").css('height', '100%');
        $("._6atl").css('width', event.data);
        $("._6ati").css('height', '100%');
        $("._6ati").css('width', event.data);
    }
    msgEvent.getMessageStarted();
    //if (event.origin === 'http://localhost:47887') {
    //    console.log(event.origin)
    //    console.log('message received:  ' + event.data, event);
    //};
}, false);

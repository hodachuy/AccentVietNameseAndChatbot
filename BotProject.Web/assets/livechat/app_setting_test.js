//const getSrcOrigin = () => new URL(document.currentScript.src).origin;
//var appDomain = "https://bot.lawcoviet.vn/"
var appDomain = "http://localhost:54160/"

var lacviet = {
    setup: function (channelGroupId) {
        lacviet.initCSS();
        lacviet.initFormChat(channelGroupId);
        //lacviet.tempPopupQnA();
        lacviet.modalContainer();

        //appDomain = decryptedUrl.slice(0, -1);
        //var modal = document.querySelector(".bot-modal");
        //var closeButton = document.querySelector(".bot-close-button");
        //function toggleModal() {
        //    modal.classList.toggle("bot-show-modal");
        //}
        //closeButton.addEventListener("click", toggleModal);
    },
    initFormChat: function (channelGroupId) {
        var styleDiv = 'z-index:2;position:fixed;bottom:0px;max-width: 100%;max-height:calc(100% - 0px);min-height:0px;min-width: 0px;background-color:transparent; border:0px;overflow:hidden;right: 0px;transition: none 0s ease 0s!important;';
        $("<div id='dialog-form-bot'\"></div>").appendTo("body");
        var html = '',
            styleIframeCustom = 'style= "width:0px;';
        styleIframeCustom += 'height:0px;';//neu mo 100%
        styleIframeCustom += 'min-height: 0px;'
        styleIframeCustom += 'min-width: 0px;'
        styleIframeCustom += 'margin: 0px;'
        styleIframeCustom += 'padding: 0px;'
        styleIframeCustom += 'background-image: none;'
        styleIframeCustom += 'background-position: 0% 0%;'
        styleIframeCustom += 'background-size: initial;'
        styleIframeCustom += 'background-attachment: scroll;'
        styleIframeCustom += 'background-origin: initial;'
        styleIframeCustom += 'background-clip: initial;'
        styleIframeCustom += 'background-color: rgba(0, 0, 0, 0);'
        styleIframeCustom += 'border-width: 0px;'
        styleIframeCustom += 'float: none;'
        styleIframeCustom += 'position: fixed;'
        styleIframeCustom += 'bottom: 0px;'
        styleIframeCustom += 'right: 10px;'
        styleIframeCustom += 'border-radius: 8px;'
        //styleIframeCustom += 'top: 30px;'
        styleIframeCustom += 'z-index: 99999;'
        styleIframeCustom += 'transition: none 0s ease 0s!important;"';
        let url = appDomain + "LcChatBox/Test?channelGroupID=" + channelGroupId + "&botId=" + channelGroupId;
        html += '<span style="vertical-align:bottom;width:0px;height:0px">';
        html += '<iframe id="dialog_iframe" class="fb_customer_chat_bounce_out_v2 name="f12691cd05677d" frameborder="0"allowtransparency="true"allowfullscreen="true"scrolling="no"';
        html += 'allow="encrypted-media" src="' + url + '" ' + styleIframeCustom + '"></iframe>';
        html += '</span>';
        html += '<div class="fb_dialog fb_dialog_advanced fb_customer_chat_bubble_pop_in fb_customer_chat_bubble_animated_with_badge fb_customer_chat_bubble_animated_no_badge" style="background: white; border-radius: 50%; bottom: 10pt;padding:15px; display: inline; position: fixed; right: 18pt; top: auto; z-index: 3; cursor:pointer">';
        html += '<div class="fb_dialog_content" style="background: none;">';
        html += '<div aria-hidden="true" class="lc-14dk0ui e1dmt1bi0"><svg color="inherit" class="lc-1mpchac" viewBox="0 0 32 32"><path fill="#4384f5" d="M12.63,26.46H8.83a6.61,6.61,0,0,1-6.65-6.07,89.05,89.05,0,0,1,0-11.2A6.5,6.5,0,0,1,8.23,3.25a121.62,121.62,0,0,1,15.51,0A6.51,6.51,0,0,1,29.8,9.19a77.53,77.53,0,0,1,0,11.2,6.61,6.61,0,0,1-6.66,6.07H19.48L12.63,31V26.46"></path><path fill="#ffffff" d="M19.57,21.68h3.67a2.08,2.08,0,0,0,2.11-1.81,89.86,89.86,0,0,0,0-10.38,1.9,1.9,0,0,0-1.84-1.74,113.15,113.15,0,0,0-15,0A1.9,1.9,0,0,0,6.71,9.49a74.92,74.92,0,0,0-.06,10.38,2,2,0,0,0,2.1,1.81h3.81V26.5Z" class="lc-p4hxbu e1nep2br0"></path></svg><div class="lc-1srqfj1 e1dmt1bi1"><div class="_4b0g"><div class="_5pd7"></div><div class="_5pd7"></div><div class="_5pd7"></div></div></div></div>';
        html += '</div>';
        html += '</div>';
        $("#dialog-form-bot").empty().append(html);
    },
    initCSS: function () {
        //var decryptedUrl = CryptoJS.AES.decrypt(encryptUrl, "Secret Passphrase").toString(CryptoJS.enc.Utf8);
        var headID = document.getElementsByTagName('head')[0];
        var link = document.createElement('link');
        link.type = 'text/css';
        link.rel = 'stylesheet';
        headID.appendChild(link);
        link.href = appDomain + "assets/livechat/css/chatbox-dialog.css";
    },
    modalContainer: function () {
        var tempModalContainer = '';
        tempModalContainer += '<div class="bot-modal" style="visibility:hidden; z-index:999999999999">';
        tempModalContainer += '        <div class="bot-modal-content large_bg">';
        tempModalContainer += '            <span class="bot-close-button">×</span>';
        tempModalContainer += '            <div id="bot-iframe">';
        tempModalContainer += '            </div>';
        tempModalContainer += '        </div>';
        tempModalContainer += '</div>';
        $(tempModalContainer).appendTo("body");
    },
    tempPopupQnA: function () {
        $('<div class="modal fade" id="cb-ques-popup" tabindex="-1" role="dialog" aria-labelledby="exampleModalLabel" aria-hidden="true" style="display:none;z-index:999999999999"></div>').appendTo("body");
        var html = '';
        html += ' <div class="modal-dialog modal-question-answer modal-sm modal-lg modal-md" role="document">';
        html += '    <div class="modal-content">';
        html += '        <div class="modal-header">';
        html += '            <button type="button" class="close" data-dismiss="modal">&times;</button>';
        html += '            <h5 class="modal-title" id="exampleModalLabel">Chi tiết câu hỏi</h5>';
        html += '        </div>';
        html += '        <div class="modal-body">';
        html += '            <form>';
        html += '                <div class="form-group">';
        html += '                    <i style="margin-right:14px" class="fa fa-question-circle icon-question-detail"></i>';
        html += '                    <label for="exampleFormControlTextarea1">CÂU HỎI</label>';
        html += '                    <div class="question-detail" id="cb-question">';
        html += '                    </div>';
        html += '                </div>';
        html += '                <div class="form-group">';
        html += '                    <i style="margin-right:10px" class="fa fa-comments icon-question-detail"></i>';
        html += '                    <label for="exampleFormControlTextarea1">TRẢ LỜI</label>';
        html += '                    <div class="answer-detail" id="cb-answer">';
        html += '                    </div>';
        html += '                </div>';
        html += '                <div class="form-group" id="cb-lst-article">';
        html += '                    <i style="margin-right:12px" class="fa fa-book icon-question-detail"></i>';
        html += '                    <label for="exampleFormControlTextarea1">ĐIỀU LUẬT LIÊN QUAN THAM KHẢO</label>';
        html += '                    <div class="text-question-detail">';
        html += '                        <div id="cb-msg"></div>';
        html += '                        <div id="cb-info-article"></div>';
        html += '                    </div>';
        html += '                </div>';
        html += '            </form>';
        html += '        </div>';
        html += '    </div>';
        html += '</div>';
        $("#cb-ques-popup").empty().append(html);
    }
};
$(document).on('click', '.fb_dialog', function (e) {
    if ($("#dialog_iframe").hasClass("fb_customer_chat_bounce_out_v2")) { // nếu đang đóng
        $("#dialog_iframe").removeClass('fb_customer_chat_bounce_out_v2').addClass('fb_customer_chat_bounce_in_v2');
        if ($(parent.window).width() <= 768) {
            //console.log($(parent.window).width())
            //$('#dialog-form-bot').css('max-height', '100%');
            //$('#dialog-form-bot').css('width', '100%');
            //$('#dialog-form-bot').css('height', '100%');
            //$('#dialog-form-bot').css('right', '0');
            //$('#dialog-form-bot').css('top', '0');
            //$('#dialog-form-bot').css('bottom', '0');

            $('#dialog_iframe').css('max-height', '100%');
            $('#dialog_iframe').css('width', '100%');
            $('#dialog_iframe').css('height', '100%');
            $('#dialog_iframe').css('right', '0');
            $('#dialog_iframe').css('top', '0');
            $('#dialog_iframe').css('bottom', '0');
            $('#dialog_iframe').css('left', '0');

            var frame = document.getElementById('dialog_iframe');
            frame.contentWindow.postMessage($(parent.window).width(), appDomain);
        }
        else {
            $('#dialog_iframe').css('width', '400px');
            $('#dialog_iframe').css('height', '580px');
        }
        setTimeout(function () {
            // bung chiều cao ô chatbox
            $('#dialog_iframe').css('max-height', '100%');
            // init message card getstarted
            var frame = document.getElementById('dialog_iframe');
            frame.contentWindow.postMessage('init', appDomain);
        }, 200)
    }
    else if ($("#dialog_iframe").hasClass("fb_customer_chat_bounce_in_v2")) { // nếu đang mở
        $("#dialog_iframe").removeClass('fb_customer_chat_bounce_in_v2').addClass('fb_customer_chat_bounce_out_v2');
        setTimeout(function () {
            //$('#dialog-form-bot').css('width', '0px');
            //$('#dialog-form-bot').css('height', '0px');
            $('#dialog_iframe').css('max-height', '0px');
        }, 200)
    }
})

var eventMethod = window.addEventListener ? "addEventListener" : "attachEvent";
var eventer = window[eventMethod];
var messageEvent = eventMethod == "attachEvent" ? "onmessage" : "message";
// Nghe tin nhắn từ iframe gởi tới trang cha khi bấm close 
eventer(messageEvent, function (e) {
    var key = e.message ? "message" : "data";
    var data = e[key];
    console.log(e.data)
    //console.log(e.origin)
    //console.log(appDomain)
    if (e.data == 'close') {
        // có tín hiệu đóng
        $('#dialog_iframe').css('max-height', '0px');
        $("#dialog_iframe").removeClass('fb_customer_chat_bounce_in_v2').addClass('fb_customer_chat_bounce_out_v2');
        //$('.fb_dialog').click();
    }
    if (e.data == 'open') {
        $('.fb_dialog').click();
    }
}, false);

// Nhận đường dẫn file export trả về
function getFileExport(exportFile) {
    console.log(exportFile)
}

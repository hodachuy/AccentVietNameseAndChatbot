var urlCreateBot = "api/bot/create",
    urlCreateFormQnA = "api/formqna/create",
    urlSettingBot = "api/setting/getbybotid",
    urlDeleteBot = "api/bot/deletebot",
    urlGetAllBot = "api/bot/getall",
    urlCloneBot = "api/bot/clone",
    urlTrainingBot = "api/bot/training",
    urlActiveBot = "Account/ActiveBotWithLivechat";//"api/bot/activeBotWithLiveChat";
var bot = {
    Name: '',
    Alias: '',
    Status: false,
    UserID: '',
}
var formQnA = {
    Name: '',
    Alias: '',
    Status: false,
    BotID: '',
    UserID: ''
}
const GROUP_SUPPORT = 3;

var applicationGroupID = $("#applicationGroupId").val();
var e;
var common = {
    init: function () {
        if (window.location.pathname.includes("/bot") == false) {
            sessionStorage.setItem("nav-active", "");
            sessionStorage.setItem("bot-id", "");
            sessionStorage.setItem("nav-active-sub", "");
        }
        if (applicationGroupID != GROUP_SUPPORT) {
            common.getBotById();
            common.registerEvents();
            common.createBot();
            common.createFormBotQnA();
            common.eventNavbar();
            common.trainingBot();
            common.cloneBotFromTemplate();
            common.cloneBotSelect();
            common.saveBotClone();
        }
    },
    eventNavbar: function () {
        //hightlight bot name
        $('#' + sessionStorage.getItem("nav-active")).attr('aria-expanded', 'true');
        $('#' + sessionStorage.getItem("nav-active")).next().addClass('show');
        $('#' + sessionStorage.getItem("nav-active")).addClass('active');
        //hightlight bot name - sub
        //$('#' + sessionStorage.getItem("nav-active-sub")).css('color', 'white');
        $('#' + sessionStorage.getItem("nav-active-sub")).addClass('active');
        //$('#' + sessionStorage.getItem("nav-active-sub")).children().css('color', 'white');

        $('body').on('click', 'li.nav-item-bot', function (e) {
            //if ($(this).children().eq(0).attr('id') !== sessionStorage.getItem("nav-active")) {
            //$('.nav-item-bot').each(function (index) {
            //    $(this).children().eq(0).next().removeClass('show');
            //    $(this).children().eq(0).removeClass('collapsed');
            //    $(this).children().eq(0).attr('aria-expanded', 'false');
            //    $(this).children().eq(0).removeClass('active');
            //    //$(this).children().eq(0).css('color', '#7a80b4')
            //})

            $(this).children().eq(0).next().removeClass('show');
            $(this).children().eq(0).removeClass('collapsed');
            $(this).children().eq(0).attr('aria-expanded', 'false');
            //$(this).children().eq(0).removeClass('active');

            //}
            var navBotID = $(this).children().eq(0).attr('id');
            //var attrBotID = $(this).children().eq(0).attr('data-id');
            if (sessionStorage.getItem("nav-active") === null) {
                sessionStorage.setItem("nav-active", navBotID);
            } else {
                if (sessionStorage.getItem("bot-id") === null) {
                    sessionStorage.setItem("nav-active", navBotID);
                }
                else if (sessionStorage.getItem("bot-id") !== null &&
                        (sessionStorage.getItem("bot-id") === $(this).children().eq(0).attr('data-id'))) {
                    sessionStorage.setItem("nav-active", navBotID);
                }
            }
            //sessionStorage.setItem("attrBotID", attrBotID);
            //$(this).children().eq(0).addClass('active');
        })
        $('body').on('click', 'li.nav-item-cog', function (e) {
            sessionStorage.setItem("nav-active", "");
            $('.nav-item-bot').each(function (index) {
                $(this).children().eq(0).next().removeClass('show');
                $(this).children().eq(0).removeClass('collapsed');
                $(this).children().eq(0).attr('aria-expanded', 'false');
                $(this).children().eq(0).css('color', '#7a80b4')
            })
        })
        $('body').on('click', 'li.nav-item-bot-sub', function (e) {
            e.stopPropagation();
            $('.nav-item-bot-sub').each(function (index) {
                $(this).children().eq(0).css('color', '#7a80b4')
                $(this).children().eq(0).children().css('color', '#7a80b4')
                $(this).children().eq(0).removeClass('active');
            })
            var elmId = $(this).children().eq(0).attr('id');
            var botId = $(this).children().eq(0).attr('data-id');
            sessionStorage.setItem("nav-active-sub", elmId);
            sessionStorage.setItem("bot-id", botId);
            var navActive = "nav-bot-id-" + botId;
            sessionStorage.setItem("nav-active", navActive);
            $(this).children().eq(0).addClass('active');
            //$(this).children().eq(0).css('color', 'white');
            //$(this).children().eq(0).children().css('color', 'white');
        })
    },
    registerEvents: function () {
        $('#btnLogout').off('click').on('click', function (e) {
            e.preventDefault();
            $('#frmLogout').submit();
        });
        $('body').on('click', '.btn-form-deploy', function (e) {
            e.stopPropagation();
            var urlApp = _Host + "static/js/app.js";
            var encryptedUrl = CryptoJS.AES.encrypt(_Host, "Secret Passphrase").toString();
            var encryptedUserID = CryptoJS.AES.encrypt($("#userId").val(), "Secret Passphrase").toString();
            var encryptedBotID = CryptoJS.AES.encrypt($(this).attr('data-botID'), "Secret Passphrase").toString();
            // pass variable outside ajax call
            var setting = function (botId) {
                var tmp = null;
                var param = {
                    botId: botId
                };
                $.ajax({
                    type: 'GET',
                    async: false,//muốn pass data ra ngoài biến nên có asynce
                    global: false,
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    url: _Host + urlSettingBot,
                    data: param,
                    success: function (data) {
                        tmp = data;
                    }
                });
                return tmp;
            }($(this).attr('data-botID'));
            var encryptedColor = CryptoJS.AES.encrypt(setting.Color, "Secret Passphrase").toString();
            var html = '';
            html += "<!--Lacviet--><script>(function (l, a, c, v, i, e, t){a[v] = a[v] || function (){";
            html += "a[v].t =+ new Date();(a[v].q = a[v].q || []).push(arguments);};i = l.createElement('script');";
            html += "var ii = l.getElementsByTagName('script')[0];i.async = 1;i.src = c;i.id = 'lacviet-script';ii.parentNode.insertBefore(i, ii);";
            html += "})(document, window, '" + urlApp + "', 'lacviet');";
            html += "setTimeout(function(){lacviet.load('" + encryptedUrl + "','" + encryptedUserID + "','" + encryptedBotID + "','" + encryptedColor + "');},1500)</script>";
            html += "<!-- End Lacviet -->"
            $('#editorTest').empty().text(html);
            $("#modalDeployBot").modal('show');
        });

        $('body').on('click', '.btn-form-delete', function () {
            var botId = $(this).attr('data-botID'),
                botName = $(this).attr('data-botName');
            console.log(botId)
            console.log(botName)
            bootbox.confirm({
                message: "Ban có chắc muốn xóa Bot " + botName + "",
                buttons: {
                    confirm: {
                        label: "Đồng ý",
                        className: 'btn-primary'
                    },
                    cancel: {
                        label: "Hủy",
                        className: 'btn-default'
                    }
                },
                callback: function (result) {
                    if (result) {
                        var params = {
                            botId: botId
                        }
                        params = JSON.stringify(params);
                        var svr = new AjaxCall(urlDeleteBot, params);
                        svr.callServicePOST(function (data) {
                            console.log(data)
                            location.reload();
                        });
                    }
                }
            })
        });

        $('body').on('click', '.switchery', function () {
            var tempNonChecked = '<span class="switchery switchery-default" style="box-shadow: rgb(223, 223, 223) 0px 0px 0px 0px inset; border-color: rgb(223, 223, 223); background-color: rgb(255, 255, 255); transition: border 0.4s ease 0s, box-shadow 0.4s ease 0s;"><small style="left: 0px; transition: background-color 0.4s ease 0s, left 0.2s ease 0s;"></small></span>';
            var tempChecked = '<span class="switchery switchery-default" style="background-color: rgb(100, 189, 99); border-color: rgb(100, 189, 99); box-shadow: rgb(100, 189, 99) 0px 0px 0px 8px inset; transition: border 0.4s ease 0s, box-shadow 0.4s ease 0s, background-color 1.2s ease 0s;"><small style="left: 19px; transition: background-color 0.4s ease 0s, left 0.2s ease 0s; background-color: rgb(255, 255, 255);"></small></span>';
            $(this).next().remove();
            if ($(this).prop('checked')) {
                $(this).next().remove();
                $(tempChecked).insertAfter($(this))
                $($(this).parent().parent().next().next()).removeClass("hidden");
            } else {
                $(this).next().remove();
                $(tempNonChecked).insertAfter($(this))
                $($(this).parent().parent().next().next()).addClass("hidden");
            }
        });
        $('body').on('change', '.chkActiveBotWithLiveChat', function () {
            var botId = $(this).parent().parent().attr('data-bot-id');
            var userId = $('#userId').val();
            var params = {
                botID: botId,
                userID: userId,
                isActiveLivechat: true
            }

            if ($(this).is(":checked")) {
                $(this).val('true');
            } else {
                $(this).val('false');
                params.isActiveLivechat = false;
            }
            params = JSON.stringify(params);
            var svr = new AjaxCall(urlActiveBot, params);
            svr.callServicePOST(function (data) {
                $("#model-notify").modal('hide');
                swal({
                    title: "Thông báo",
                    text: data.message,
                    confirmButtonColor: "#EF5350",
                    type: "success"
                }, function () { $("#model-notify").modal('show'); });
            });
        })
    },
    getSeoTitle: function (input) {
        if (input == undefined || input == '')
            return '';
        //Đổi chữ hoa thành chữ thường
        var slug = input.toLowerCase();

        //Đổi ký tự có dấu thành không dấu
        slug = slug.replace(/á|à|ả|ạ|ã|ă|ắ|ằ|ẳ|ẵ|ặ|â|ấ|ầ|ẩ|ẫ|ậ/gi, 'a');
        slug = slug.replace(/é|è|ẻ|ẽ|ẹ|ê|ế|ề|ể|ễ|ệ/gi, 'e');
        slug = slug.replace(/i|í|ì|ỉ|ĩ|ị/gi, 'i');
        slug = slug.replace(/ó|ò|ỏ|õ|ọ|ô|ố|ồ|ổ|ỗ|ộ|ơ|ớ|ờ|ở|ỡ|ợ/gi, 'o');
        slug = slug.replace(/ú|ù|ủ|ũ|ụ|ư|ứ|ừ|ử|ữ|ự/gi, 'u');
        slug = slug.replace(/ý|ỳ|ỷ|ỹ|ỵ/gi, 'y');
        slug = slug.replace(/đ/gi, 'd');
        //Xóa các ký tự đặt biệt
        slug = slug.replace(/\`|\~|\!|\@|\#|\||\$|\%|\^|\&|\*|\(|\)|\+|\=|\,|\.|\/|\?|\>|\<|\'|\"|\:|\;|_/gi, '');
        //Đổi khoảng trắng thành ký tự gạch ngang
        slug = slug.replace(/ /gi, "-");
        //Đổi nhiều ký tự gạch ngang liên tiếp thành 1 ký tự gạch ngang
        //Phòng trường hợp người nhập vào quá nhiều ký tự trắng
        slug = slug.replace(/\-\-\-\-\-/gi, '-');
        slug = slug.replace(/\-\-\-\-/gi, '-');
        slug = slug.replace(/\-\-\-/gi, '-');
        slug = slug.replace(/\-\-/gi, '-');
        //Xóa các ký tự gạch ngang ở đầu và cuối
        slug = '@' + slug + '@';
        slug = slug.replace(/\@\-|\-\@|\@/gi, '');

        return slug;
    },
    templateBot: function (data) {
        var html = '';
        html += '<div class="col-lg-4 col-md-6 col-12 bot-role-1" style="padding-bottom: 15px;">';
        html += '        <div class="dropdown dropleft btn-bot-setting-home show">';
        html += '            <button data-toggle="dropdown" aria-haspopup="true" aria-expanded="true" title="Menu" class="icon-btn btn-menu-bot">';
        html += '                <i class="icon-dots-vertical fas fa-ellipsis-h"></i>';
        html += '            </button>';
        html += '            <ul class="dropdown-menu dropdown-menu-right hide" x-placement="left-start" style="cursor: auto; box-shadow: rgb(220, 220, 220) 2px 2px 10px; position: absolute; will-change: transform; top: 0px; left: 45px; transform: translate3d(-212px, 3px, 0px);">';
        html += '                <li>';
        html += '                     <i class="icon-scenarios fa fa-file"></i>';
        html += '                    <span class="bot-setting-option">Kịch bản</span>';
        html += '                </li>';
        html += '                <li>';
        html += '                    <i class="icon-nlp fa fa-history"></i>';
        html += '                    <span class="bot-setting-option">Lịch sử</span>';
        html += '                </li>';
        html += '                <li>';
        html += '                    <i class="icon-message1 fa fa-cog"></i>';
        html += '                    <span class="bot-setting-option">Cài đặt</span>';
        html += '                </li>';
        html += '                <li style="color: rgb(255, 0, 0);">';
        html += '                    <i class="icon-trash fa fa-trash" style="color: rgb(255, 0, 0);"></i>';
        html += '                    <span class="bot-setting-option btn-form-delete" data-botID="' + data.ID + '" data-botName="' + data.Name + '" >Xóa Bot</span>';
        html += '                </li>';
        html += '            </ul>';
        html += '        </div>';
        html += '        <a href="/bot/setting/' + data.Alias + '/' + data.ID + '?name=' + data.Name + '">';
        html += '            <div class="bot-style">';
        html += '                <div class="bot-header">';
        html += '                    <div class="bot-icon" style="background-color: Color [A=255, R=224, G=192, B=253]; color: rgb(0, 0, 0);">';
        var arrName = data.Name.split(" ");
        if (arrName.length > 1) {
            var nameAcronym = arrName[0].substring(0, 1).toUpperCase() + arrName[1].substring(0, 1).toUpperCase();
            html += nameAcronym;
        } else {
            html += arrName[0].substring(0, 1).toUpperCase();
        }
        html += '                    </div>';
        html += '                    <span title="' + data.Name.toUpperCase() + '" class="bot-dp-name">' + data.Name.toUpperCase() + '</span>';
        html += '                    <span class="bot-dp-lang"><i class="flag-icon flag-icon-gb"></i> Tiếng Việt</span>';
        html += '                </div>';
        html += '                <div class="bot-content">';
        html += '                    <div class="row">';
        html += '                        <div title="Ý định" class="col-6" style="padding-right: 0px;">Ý định</div>';
        html += '                        <div class="col-6">0</div>';
        html += '                        <div title="Loại thực thể" class="col-6" style="padding-right: 0px;">Loại thực thể</div>';
        html += '                        <div class="col-6">0</div>';
        html += '                        <div title="Câu mẫu" class="col-6" style="border-bottom: none;">Câu kịch bản </div>';
        html += '                        <div class="col-6" style="border-bottom: none;">0</div>';
        html += '                    </div>';
        html += '                </div>';
        html += '                <div class="bot-footer">';
        html += '                    <div class="row">';
        html += '                        <div title="Lần huấn luyện gần nhất" class="col-6">Lần huấn luyện gần nhất</div>';
        html += '                        <div class="col-6"></div>';
        html += '                    </div>';
        html += '                </div>';
        html += '                <div class="bot-footer">';
        html += '                    <div class="row">';
        html += '                        <div title="Hoạt động" class="col-6">Đang hoạt động</div>';
        html += '                        <div class="col-6">';
        html += '                            <span><img title="Website" src="/assets/images/img-website.jpg" class="img-bot-activing"></span>';
        html += '                        </div>';
        html += '                    </div>';
        html += '                </div>';
        html += '            </div>';
        html += '        </a>';
        html += '    </div>';
        return html;
    },
    createBot: function () {
        var temp = function (data) {
            var html = '';
            html += '<li class="nav-item nav-item-bot">';
            html += '<a class="nav-link collapsed" id="nav-bot-id-' + data.ID + '" data-id="' + data.ID + '" href="#" data-toggle="collapse" aria-expanded="false" data-target="#submenu-' + data.ID + '" aria-controls="submenu-' + data.ID + '">';
            html += '<i class="fa fa fa-robot" aria-hidden="true"></i> ' + data.Name.toUpperCase() + '';
            html += '</a>';
            html += '<div id="submenu-' + data.ID + '" class="collapse submenu" style="">';
            html += '<ul class="nav flex-column">';
            html += '<li class="nav-item nav-item-bot-sub">';
            html += '<a class="nav-link" data-id="' + data.ID + '" href="' + _Host + 'bot/' + data.Alias + '/' + data.ID + '/cardcategory?botName=' + data.Name + '" id="bot-card-' + data.ID + '"><i class="fa fa-plus-circle" aria-hidden="true"></i>Tạo thẻ tin nhắn</a>';
            html += '</li>';
            html += '<li class="nav-item nav-item-bot-sub">';
            html += '<a class="nav-link" data-id="' + data.ID + '" href="' + _Host + 'bot/' + data.Alias + '/' + data.ID + '/module?botName=' + data.Name + '" id="bot-module-' + data.ID + '"><i class="fa fa-plug" aria-hidden="true"></i>Tích hợp Module</a>';
            html += '</li>';
            html += '<li class="nav-item nav-item-bot-sub">';
            html += '<a class="nav-link" href="javascript:void(0)" id="btnCreateBotQnAnswer" data-botId="' + data.ID + '" data-botName="' + data.Name + '"><i class="fa fa-brain"></i>Tri thức bot';
            html += '<span style="float: right;color: lightgray;cursor: pointer;">';
            html += '<i class="fa fa-plus-circle fa-icons-right" aria-hidden="true"></i>';
            html += '</span>';
            html += '</a>';
            html += '<div class="submenu" style="">';
            html += '<ul class="nav flex-column" id="form-bot-qna-' + data.ID + '">';
            html += '</ul>';
            html += '</div>';
            html += '</li>';
            html += '<li class="nav-item nav-item-bot-sub">';
            html += '<a class="nav-link" data-id="' + data.ID + '" href="' + _Host + 'bot/searchengine/' + data.Alias + '/' + data.ID + '?botName=' + data.Name + '" id="bot-search-' + data.ID + '"><i class="fa fa-search" aria-hidden="true"></i>Search Engineer</a>';
            html += '</li>';
            html += '<li class="nav-item nav-item-bot-sub">';
            html += '<a class="nav-link" data-id="' + data.ID + '" href="' + _Host + 'bot/setting/' + data.Alias + '/' + data.ID + '?name=' + data.Name + '" id="bot-setting-' + data.ID + '"><i class="fa fa-cog" aria-hidden="true"></i>Cài đặt</a>';
            html += '</li>';
            html += '<li class="nav-item nav-item-bot-sub">';
            html += '    <a class="nav-link" href="#" id="bot-training-' + data.ID + '"><i class="fa fa-recycle" aria-hidden="true"></i>Huấn luyện</a>';
            html += '</li>';
            //html += '<li class="nav-item">';
            //html += '<a class="nav-link btn-form-deploy" href="javascript:void(0);" data-botID="' + data.ID + '"><i class="fa fa-rocket" aria-hidden="true"></i>Deploy API</a>';
            //html += '</li>';
            //html += '<li class="nav-item nav-item-bot-sub">';
            //html += '<a class="nav-link btn-form-delete" href="javascript:void(0);" data-botID="' + data.ID + '" data-botName="' + data.Name + '"><i class="fa fa-trash" aria-hidden="true"></i>Xóa bot</a>';
            //html += '</li>';
            html += '</ul>';
            html += '</div>';
            html += '</li>';
            return html;
        }

        $('body').on('click', '#btnCreateBot', function () {
            $('#txtBotName').val('');
            $('#modalCreateBot').modal('show');
        })
        $('body').on('click', '#btnSaveBot', function () {
            var botName = $('#txtBotName').val();
            if (botName == '' || botName == undefined)
                return false;
            bot.Name = botName;
            bot.Status = true;
            bot.Alias = common.getSeoTitle(botName);
            bot.UserID = $('#userId').val();
            var svr = new AjaxCall(urlCreateBot, JSON.stringify(bot));
            svr.callServicePOST(function (data) {
                var tempHtml = common.templateBot(data);
                $('#collapseDiv_0').append(tempHtml);
                //$('#bot-category').append(tempHtml);
                $('#modalCreateBot').modal('hide');
            });
        })
        $('body').on('click', '.new-blank-bot', function (e) {
            $('#txtBotTemplate').val('');
            $('#modalCreateBot').modal('show');
        })

        $('body').on('click', '#btnCancleSaveBot', function (e) {
            $('#modalCreateBot').modal('hide');
        })
    },
    cloneBotFromTemplate: function () {
        $('body').on('click', '.card-tmp-bot', function (e) {
            if ($('#userId').val() == "4d1d77aa-42a7-4e88-97a6-baed104c2e60") {
                swal({
                    title: "Thông báo",
                    text: "Chức năng này không dành cho tài khoản quản trị",
                    confirmButtonColor: "#EF5350",
                    type: "success"
                }, function () {
                    $("#model-notify").modal('show');
                    return false;
                });
                return false;
            }
            var txtBotTemplate = $(this).find('.card-title').text();
            var txtBotTemplateId = $(this).find('.card-title').eq(0).attr('data-bot-id');
            $("#txtBotTemplateId").val(txtBotTemplateId);
            $("#txtBotTemplateTitle").val(txtBotTemplate);
            $("#txtBotCloneName").val('');
            $('#modalBotClone').modal('show');
        })
    },
    cloneBotSelect: function () {
        $('body').on('click', '.btn-clone-bot', function (e) {
            var txtBotTemplateId = $(this).attr('data-bot-id');
            $("#txtBotTemplateId").val(txtBotTemplateId);
            $("#txtBotCloneName").val('');
            $('#modalBotClone').modal('show');
        })
    },
    saveBotClone:function(){
        $('body').on('click', '#btnSaveBotClone', function () {
            //$('#modalBotClone').modal('hide');
            //$('#modalBotCloneProgessBar').modal('show');         
            //$('#modalBotCloneProgessBar .modal-body').empty().append('<div class="progress"></div><span style="float:right" id="percent">a</span>')
            //var botName = $('#txtBotCloneName').val();
            //var botId = $("#txtBotTemplateId").val();
            //if (botName == '' || botName == undefined)
            //    return false;
            //if (botId == '' || botId == undefined)
            //    return false;
            //var params = {
            //    botId: botId,
            //    userId: $('#userId').val(),
            //    botName: botName,
            //    botAlias: common.getSeoTitle(botName)
            //}
            //params = JSON.stringify(params);
            //$.ajax({
            //    xhr: function () {
            //        var xhr = new window.XMLHttpRequest();
            //        xhr.upload.addEventListener("progress", function (evt) {
            //            if (evt.lengthComputable) {
            //                var percentComplete = evt.loaded / evt.total;
            //                console.log(percentComplete);
            //                $("#percent").text(Math.floor(percentComplete * 100) + '%')
            //                $('.progress').css({
            //                    width: percentComplete * 100 + '%'
            //                });
            //                if (percentComplete === 1) {
            //                    $('.progress').addClass('hide');
            //                }
            //            }
            //        }, false);
            //        xhr.addEventListener("progress", function (evt) {
            //            if (evt.lengthComputable) {
            //                var percentComplete = evt.loaded / evt.total;
            //                console.log(percentComplete);

            //                $('.progress').css({
            //                    width: percentComplete * 100 + '%'
            //                });
            //            }
            //        }, false);
            //        return xhr;
            //    },
            //    type: 'POST',
            //    url: _Host + urlCloneBot,
            //    data: params,
            //    contentType: "application/json; charset=utf-8",
            //    dataType: "json",
            //    success: function (response) {
            //        if (response.status) {
            //                $('#modalBotCloneProgessBar .modal-footer').empty().append('<a href="#" class="btn btn-success" id="btnCancleSaveBotClone">Thành công</a>')
            //                var tempHtml = common.templateBot(response.data);
            //                $('#collapseDiv_0').append(tempHtml);
            //                $("#model-notify").modal('hide');
            //                swal({
            //                    title: "Thông báo",
            //                    text: "Thêm thành công",
            //                    confirmButtonColor: "#EF5350",
            //                    type: "success"
            //                }, function () { $("#model-notify").modal('show'); });
            //            } else {
            //                alert("Lưu thất bại, vui lòng liên hệ quản trị");
            //                console.log(response.data)
            //            }
            //    }
            //});
            $('#modalBotClone').modal('hide');
            var botName = $('#txtBotCloneName').val();
            var botId = $("#txtBotTemplateId").val();
            if (botName == '' || botName == undefined)
                return false;
            if (botId == '' || botId == undefined)
                return false;
            var params = {
                botId: botId,
                userId: $('#userId').val(),
                botName: botName,
                botAlias: common.getSeoTitle(botName)
            }
            params = JSON.stringify(params);
            var svr = new AjaxCall(urlCloneBot, params);
            svr.callServicePOST(function (response) {
                if (response.status) {
                    var tempHtml = common.templateBot(response.data);
                    $('#collapseDiv_0').append(tempHtml);
                    $("#model-notify").modal('hide');
                    swal({
                        title: "Thông báo",
                        text: "Thêm thành công",
                        confirmButtonColor: "#EF5350",
                        type: "success"
                    }, function () { $("#model-notify").modal('show'); });
                } else {
                    alert("Lưu thất bại, vui lòng liên hệ quản trị");
                    console.log(response.data)
                }
            });
        })
        $('body').on('click', '#btnCancleSaveBotClone', function (e) {
            $('#modalBotClone').modal('hide');
        })
    },
    createFormBotQnA: function () {
        var temp = function (data, botId, botName) {
            var html = '';
            html += '<li class="nav-item">';
            html += '<a class="nav-link bot-qna-link" data-id="' + botId + '" href="' + _Host + 'bot/qna?formQnAId=' + data.ID + '&botId=' + botId + '&botName=' + botName + '" id="bot-scenarios-' + botId + '-' + data.ID + '"><i class="fa fa-file" aria-hidden="true" style="display:unset"></i>' + data.Name + '</a>';
            html += '</li>';
            return html;
        }
        $('body').on('click', '#btnCreateBotQnAnswer', function (e) {
            e.stopPropagation();
            var botID = $(this).attr('data-botId');
            var botName = $(this).attr('data-botName');
            $('#txtBotQnAnswerName').val('');
            $('#modalCreateBotQnAnswer').modal('show');
            $("#bot-botQnA-id").val(botID);
            $("#bot-botQnA-name").val(botName);
        })
        $('body').on('click', '#btnSaveBotQnA', function () {
            var formName = $('#txtBotQnAnswerName').val();
            if (formName == '' || formName == undefined)
                return false;
            formQnA.Name = formName;
            formQnA.Alias = common.getSeoTitle(formName);
            formQnA.BotID = $("#bot-botQnA-id").val();
            formQnA.UserID = $("#userId").val();
            console.log(formQnA)
            var svr = new AjaxCall(urlCreateFormQnA, JSON.stringify(formQnA));
            svr.callServicePOST(function (data) {
                var tempHtml = temp(data, formQnA.BotID, $("#bot-botQnA-name").val());
                $('#form-bot-qna-' + formQnA.BotID).append(tempHtml);
                $('#modalCreateBotQnAnswer').modal('hide');
                window.location.href = _Host + 'bot/qna?formQnAId=' + data.ID + "&botId=" + formQnA.BotID + "&botName=" + $("#bot-botQnA-name").val();
            });
        })
    },
    trainingBot: function () {
        $('body').on('click', '#btnTrainingBot', function () {
            var botId = $(this).attr('data-botID');
            var params = {
                botId: botId
            }
            params = JSON.stringify(params);
            var svr = new AjaxCall(urlTrainingBot, params);
            svr.callServicePOST(function (data) {
                if (data.status) {
                    swal({
                        title: "Thông báo",
                        text: data.message,
                        confirmButtonColor: "#EF5350",
                        type: "success"
                    }, function () { $("#model-notify").modal('show'); });
                } else {
                    swal({
                        title: "Thông báo",
                        text: data.message,
                        confirmButtonColor: "#EF5350",
                        type: "error"
                    }, function () { $("#model-notify").modal('show'); });
                }
            });
        })
    },
    getBotById: function () {
        var botId = $("#botId").val();
        if (botId != undefined) {
            var param = {
                userId: $("#userId").val()
            }
            var svr = new AjaxCall(urlGetAllBot, param);
            svr.callServiceGET(function (response) {
                if (response.length != 0) {
                    $(".nav-divider").hide();
                    var botVm = response.filter(function (x) { return x.ID == botId; });
                    var data = botVm[0];
                    var html = '';
                    html += '<li class="nav-item nav-item-bot">';
                    html += '<a class="nav-link active" id="nav-bot-id-' + data.ID + '" data-id="' + data.ID + '" href="#" data-toggle="collapse" aria-expanded="false" data-target="#submenu-' + data.ID + '" aria-controls="submenu-' + data.ID + '">';
                    html += '<img src="' + _Host + '/assets/images/logo/icon-bot-v2.png" style="width:40px;height:30px"/>BOT - ' + data.Name.toUpperCase() + '';
                    html += '</a>';
                    html += '<div id="submenu-' + data.ID + '" class="submenu collapse show" style="">';
                    html += '<ul class="nav flex-column">';
                    html += '<li class="nav-item nav-item-bot-sub">';
                    html += '<a class="nav-link" data-id="' + data.ID + '" href="' + _Host + 'bot/' + data.Alias + '/' + data.ID + '/cardcategory?botName=' + data.Name + '" id="bot-card-' + data.ID + '"><i class="fa fa-plus-circle" aria-hidden="true"></i>Tạo thẻ tin nhắn</a>';
                    html += '</li>';
                    html += '<li class="nav-item nav-item-bot-sub">';
                    html += '<a class="nav-link" data-id="' + data.ID + '" href="' + _Host + 'bot/' + data.Alias + '/' + data.ID + '/module?botName=' + data.Name + '" id="bot-module-' + data.ID + '"><i class="fa fa-plug" aria-hidden="true"></i>Tích hợp Module</a>';
                    html += '</li>';
                    html += '<li class="nav-item nav-item-bot-sub">';
                    html += '<a class="nav-link" href="javascript:void(0)" id="btnCreateBotQnAnswer" data-botId="' + data.ID + '" data-botName="' + data.Name + '"><i class="fa fa-brain"></i>Tri thức bot';
                    html += '<span style="float: right;color: lightgray;cursor: pointer;">';
                    html += '<i class="fa fa-plus-circle fa-icons-right" aria-hidden="true"></i>';
                    html += '</span>';
                    html += '</a>';
                    html += '<div class="submenu" style="">';
                    html += '<ul class="nav flex-column" id="form-bot-qna-' + data.ID + '">';
                    if (data.FormQuestionAnswers.length != 0) {
                        $.each(data.FormQuestionAnswers, function (index, value) {
                            html += '<li class="nav-item nav-item-bot-sub">';
                            html += '<a class="nav-link bot-qna-link" data-id="' + value.ID + '" id="bot-scenarios-' + data.ID + '-' + value.ID + '" href="' + _Host + 'bot/qna/?formQnAId=' + value.ID + '&botId=' + data.ID + '&botName=' + data.Name + '"><i class="fa fa-file-alt" aria-hidden="true" style="display:unset"></i>' + value.Name + '</a>';
                            html += '</li>';
                        })
                    }
                    html += '</ul>';
                    html += '</div>';
                    html += '</li>';
                    html += '<li class="nav-item nav-item-bot-sub">';
                    html += '<a class="nav-link" data-id="' + data.ID + '" href="' + _Host + 'bot/searchengine/' + data.Alias + '/' + data.ID + '?botName=' + data.Name + '" id="bot-search-' + data.ID + '"><i class="fa fa-search" aria-hidden="true"></i>Search Engineer</a>';
                    html += '</li>';
                    if (data.ID == 3019) {
                        html += '    <li class="nav-item nav-item-bot-sub">';
                        html += '        <a class="nav-link" href="' + _Host + 'bot/medicalsymptoms/' + data.Alias + '/' + data.ID + '?botName=' + data.Name + '" data-id="' + data.ID + '" id="bot-med-' + data.ID + '"><i class="fa fa-briefcase-medical" aria-hidden="true"></i>Triệu chứng y tế</a>';
                        html += '    </li>';
                    }
                    html += '<li class="nav-item nav-item-bot-sub">';
                    html += '<a class="nav-link" data-id="' + data.ID + '" href="' + _Host + 'bot/setting/' + data.Alias + '/' + data.ID + '?name=' + data.Name + '" id="bot-setting-' + data.ID + '"><i class="fa fa-cog" aria-hidden="true"></i>Cài đặt</a>';
                    html += '</li>';
                    //html += '<li class="nav-item">';
                    //html += '<a class="nav-link btn-form-deploy" href="javascript:void(0);" data-botID="' + data.ID + '"><i class="fa fa-rocket" aria-hidden="true"></i>Deploy API</a>';
                    //html += '</li>';
                    html += '<li class="nav-item nav-item-bot-sub">';
                    html += '    <a class="nav-link" data-botID="' + data.ID + '" href="#" id="btnTrainingBot"><i class="fa fa-recycle" aria-hidden="true"></i>Huấn luyện</a>';
                    html += '</li>';
                    html += '<li class="nav-item nav-item-bot-sub">';
                    html += '    <a class="nav-link" href="' + _Host + 'bot/history/' + data.Alias + '/' + data.ID + '?botName=' + data.Name + '" data-id="' + data.ID + '" id="bot-history-' + data.ID + '"><i class="fa fa-history" aria-hidden="true"></i>Lịch sử</a>';
                    html += '</li>';
                    //html += '<li class="nav-item nav-item-bot-sub">';
                    //html += '<a class="nav-link btn-form-delete" href="javascript:void(0);" data-botID="' + data.ID + '" data-botName="' + data.Name + '"><i class="fa fa-trash" aria-hidden="true"></i>Xóa bot</a>';
                    //html += '</li>';
                    html += '</ul>';
                    html += '</div>';
                    html += '</li>';
                    $('#bot-category').empty().append(html);

                    common.eventNavbar();

                    // Dropdown list category bot
                    var tempHeaderBotCategory = '';
                    tempHeaderBotCategory += '<ul class="navbar-nav mr-auto navbar-left-top">';
                    tempHeaderBotCategory += '<li class="nav-item dropdown nav-bot">';
                    tempHeaderBotCategory += '<a class="nav-link nav-user-img" href="#" id="navbarDropdownMenuLink2" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" style="color:aliceblue;border:1px solid darkgray;margin-left: 10px;">';
                    tempHeaderBotCategory += '<i class="fa fa fa-robot mr-2" aria-hidden="true"></i>';
                    tempHeaderBotCategory += data.Name.toUpperCase();
                    tempHeaderBotCategory += '<i class="fa fa-angle-down ml-2 opacity-5"></i>';
                    tempHeaderBotCategory += '</a>';
                    tempHeaderBotCategory += '<div class="dropdown-menu nav-user-dropdown" aria-labelledby="navbarDropdownMenuLink2" style="left:auto;padding-top: 10px;margin-top: 3px;">';
                    $.each(response, function (index, value) {
                        if (value.ID == botId) {
                            tempHeaderBotCategory += '<a class="dropdown-item" href="javascript:void(0);"><i class="fa fa fa-robot mr-2"></i>' + value.Name.toUpperCase() + '<i class="fa fa fa-check ml-2"></i></a>';
                        } else {
                            tempHeaderBotCategory += '<a class="dropdown-item" href="' + _Host + 'bot/setting/' + value.Alias + '/' + value.ID + '?name=' + value.Name + '"><i class="fa fa fa-robot mr-2"></i>' + value.Name.toUpperCase() + '</a>';
                        }
                    })
                    tempHeaderBotCategory += '</div>';
                    tempHeaderBotCategory += '</li>';
                    tempHeaderBotCategory += '</ul>';
                    $("#navBotCategory").empty().append(tempHeaderBotCategory);
                }
            });
        }
    }
}
common.init();


/*
**** Danh sách thẻ để chọn
*/
var elementCard;
var modalCard = {
    modalEvent: function (elm) {
        // action form modal area      
        $("body").on('click', elm, function (e) {
            $("#modalListCard").modal({
                backdrop: 'static',
                keyboard: true,
                show: true
            });
            $("#txt-search-card-name").val('');
            elementCard = $(this);
            new modalCard.card().GetListCardSelect(1, 10, "");
        })
        $(".btn-close-modal-list-card").off().on('click', function () {
            $("#modalListCard").modal('hide');
        })
    },
    card: function () {
        this.GetListCardSelect = function (page, pageSize, cardName) {
            if (cardName == undefined) {
                cardName = "";
            }
            var param = {
                cardName: cardName,
                botId: $("#botId").val(),
                page: page,
                pageSize: pageSize,
            }
            $.ajax({
                url: _Host + 'api/card/getListCardSelect',
                contentType: 'application/json; charset=utf-8',
                data: param,
                type: 'GET',
                success: function (result) {
                    new modalCard.card().RenderTable(result);
                },
            });
        },
        this.SetValueCard = function (cardId, cardName) {
            elementCard.children().attr('data-card-id', cardId);
            elementCard.children().attr('title', cardName);
            elementCard.find('.filter-option-inner-inner').html(cardName);
            $("#modalListCard").modal('hide');
        },
        this.RenderTable = function (data) {
            var dataTable = data.Items;
            var html = '';
            var paginationListHtml = '';
            paginationListHtml += '<li class="pagination__group1"><a href="javascript:void(0);" class="pagination__item pagination__control pagination__control_prev">prev</a></li>';
            paginationListHtml += '<li class="pagination__group1"><a href="#0" class="pagination__item">1</a></li>';
            paginationListHtml += '<li class="pagination__group1"><a href="#0" class="pagination__item pagination__control pagination__control_next">next</a></li>';

            var paginationTextHtml = '';
            var fromPP = 0;
            var toPP = 0;
            var totalPP = 0;
            var pageSize = data.MaxPage;

            var txtCardNameSearch = $("#txt-search-card-name").val();
            if (dataTable.length != 0) {
                // đếm số index
                var iCount = 1;
                if (data.Page > 1) {
                    iCount = ((data.Page - 1) * pageSize) + 1;
                }

                $.each(dataTable, function (index, item) {
                    //index = index + 1;
                    html += '<tr>';
                    html += '<td>' + (iCount++) + '</td>';
                    html += '<td>' + (item.GroupCardName.length > 100 ? item.GroupCardName.substring(0, 100) + '...' : item.GroupCardName) + '</td>';
                    html += '<td>' + item.Name + '</td>';
                    html += '<td><a href="javascript:new modalCard.card().SetValueCard(\'' + item.ID + '\',\'' + item.Name + '\')" class="action-area"><i class="fa fa-check"></i></a></td>';
                    html += '</tr>';
                })

                // khoản cách mRangePage <--> " so page chọn" <--> mRangePage
                var mRangePage;
                if (data.MaxPage == 5) {
                    mRangePage = data.MaxPage / 2;
                } else {
                    mRangePage = data.MaxPage / (data.MaxPage / 2);
                }
                // render pagination : phân trang table
                var startPageIndex = Math.max(1, (data.Page - mRangePage));
                var endPageIndex = Math.min(data.TotalPages, (data.Page + mRangePage));
                var firstPage = 1;
                var lastPage = data.TotalPages;
                var previousPage = data.Page - 1;
                var nextPage = data.Page + 1;

                paginationListHtml = '';
                if (data.Page > firstPage) {
                    paginationListHtml += '<li class="pagination__group1"><a href="javascript:void(0);" onclick="return new modalCard.card().GetListCardSelect(' + (data.Page - 1) + ',' + pageSize + ',\'' + txtCardNameSearch + '\')" class="pagination__item pagination__control pagination__control_prev">prev</a></li>';
                }
                for (var i = startPageIndex; i <= endPageIndex; i++) {
                    if (data.Page == i) {
                        paginationListHtml += '<li class="pagination__group1"><span class="pagination__item pagination__item_active">' + i + '</span></li>';
                    }
                    else {
                        paginationListHtml += '<li class="pagination__group1"><a href="javascript:void(0);" onclick="return new modalCard.card().GetListCardSelect(' + i + ',' + pageSize + ',\'' + txtCardNameSearch + '\')" class="pagination__item">' + i + '</a></li>';
                    }
                }
                if (data.Page < lastPage) {
                    paginationListHtml += '<li class="pagination__group1"><a href="javascript:void(0);" onclick="return new modalCard.card().GetListCardSelect(' + (data.Page + 1) + ',' + pageSize + ',\'' + txtCardNameSearch + '\')" class="pagination__item pagination__control pagination__control_next">next</a></li>';
                }

                // thông tin số trang
                fromPP = ((data.Page - 1) * pageSize) + 1;
                toPP = ((data.Page - 1) * pageSize) + dataTable.length;
                totalPP = data.TotalCount;
            }

            $("#table-data-card").empty().append(html);
            $("#pagination-list-card").empty().append(paginationListHtml);

            paginationTextHtml = 'từ <span style="font-weight: bold">' + fromPP + '</span> đến <span style="font-weight: bold">' + toPP + ' </span>trong <span style="font-weight: bold">' + totalPP + ' </span>mục';
            $("#pagination-card-number-show").empty().append(paginationTextHtml);

            // scroll top table
            document.getElementById("table-card").scrollTop = 0;

            // select number show table
            $("select#results-pp-card").change(function () {
                pageSize = $(this).children("option:selected").val();
                var cardName = $("#txt-search-card-name").val();
                new modalCard.card().GetListCardSelect(1, pageSize, cardName);
            });
            // writing show suggest
            $("#btnCardSearch").click(function (e) {
                var cardName = $("#txt-search-card-name").val();
                if (cardName != undefined || cardName != "") {
                    new modalCard.card().GetListCardSelect(1, pageSize, cardName);
                }
            })
            $("#btnCardRefresh").click(function (e) {
                $("#txt-search-card-name").val('');
                new modalCard.card().GetListCardSelect(1, pageSize, '');
            })
        }
    }
}

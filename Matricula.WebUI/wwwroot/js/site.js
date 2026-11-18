// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
  function alertPopup(message) {
    $("#dialog-confirm .alert_content").text(message);
    $("#dialog-confirm")[0].showModal();
  }

  $(".btn_close_modal").click(function () {
    $("#dialog-confirm")[0].close();
  });

  $("#registro_matricula").validate({
    ignore: [],
    rules: {
      studentSelect: {
        required: true,
      },
      shiftSelect: {
        required: true,
      },
      listCourses: "required",
    },
  });

  $("#register").click(function (e) {
    e.preventDefault();

    $("#registro_matricula").submit();
  });

  $("#register").submit(function (e) {

    //stop submitting the form to see the disabled button effect
    e.preventDefault();
  });

  function loadCourse(cycle = 0, shiftId = "") {
    if (cycle > 0 && shiftId != "" && shiftId != "0") {
      $.ajax({
        url: "/Home/GetCourses",
        type: "POST",
        dataType: "json",
        data: {
          cycle: cycle,
          shiftId: shiftId,
        },
        success: function (data) {
          if (data.length > 0) {
            var options = $.map(data, function (value) {
              return $("<option>")
                .val(value.id)
                .text(value.courseName + " (" + value.startTime + " - " + value.endTime + ")")
                .attr("data-endtime", value.endTime)
                .attr("data-starttime", value.startTime)
                .attr("data-classroom", value.classRoomName)
                .attr("data-capacity", value.capacity)
                .attr("data-teacher", value.teacherName)
                .attr("data-shift", value.shiftName);
            });
            $("#courseSelect").empty().append(options);
          } else {
            alertPopup("Para el ciclo y turno seleccionado no hay cursos disponibles");
          }
        },
      });
    } else {
      $("#courseSelect").empty().append($("<option>").val("0").text("Seleccionar Curso"));
    }
  }

  function clearSelectedCourses() {
    $("#courses tr").remove();
    updateListCourses();
  }

  $("#studentSelect").change(function () {
    var studentId = $(this).val();

    clearSelectedCourses();
    if (!studentId) {
      loadCourse(0, "");
      return;
    }

    loadCourse($("#studentSelect option:selected").data("cycle"), $("#shiftSelect option:selected").val());
  });

  $("#shiftSelect").change(function () {
    var shiftId = $(this).val();

    clearSelectedCourses();
    loadCourse($("#studentSelect option:selected").data("cycle"), shiftId);
  });

  $(document).on("click", ".no", function () {
    var tr = $(this).closest("tr");
    var value = tr.find(".listCoursesTemp").val();
    var title = tr.find(".titleCourse").text();

    var capacity = tr.find(".capacity").text();
    var classRoom = tr.find(".classRoom").text();
    var startTime = tr.find(".startTime").text();
    var endTime = tr.find(".endTime").text();
    var teacher = tr.find(".teacher").text();

    $("#courseSelect").append(
      $("<option>")
        .val(value)
        .text(title)
        .attr("data-endtime", endTime)
        .attr("data-starttime", startTime)
        .attr("data-classroom", classRoom)
        .attr("data-capacity", capacity)
        .attr("data-teacher", teacher)
    );

    $(this).closest("tr").remove();
    updateListCourses();
  });

  function updateListCourses() {
    var values = $("input[name='listCoursesTemp[]']")
      .map(function () {
        return $(this).val();
      })
      .get();

    $("#listCourses").val(values);
  }

  var row;

  $(".btn_add").click(function () {
    $(".error_course").hide();
    $("#courseSelect").removeClass("error");
    if ($("#courseSelect option:selected").val()) {
      var scheduleCurrent =
        $("#courseSelect option:selected").data("starttime") +
        "-" +
        $("#courseSelect option:selected").data("endtime");

      var toMinutes = function (t) {
        var p = String(t).split(":");
        return parseInt(p[0], 10) * 60 + parseInt(p[1], 10);
      };
      var newStart = toMinutes($("#courseSelect option:selected").data("starttime"));
      var newEnd = toMinutes($("#courseSelect option:selected").data("endtime"));
      var conflict = false;
      $("#courses tr").each(function () {
        var exStart = toMinutes($(this).find(".startTime").text());
        var exEnd = toMinutes($(this).find(".endTime").text());
        if (newStart < exEnd && exStart < newEnd) {
          conflict = true;
          return false;
        }
      });

      if (conflict) {
        alertPopup("El curso que trata de añadir coincide en el horario con otro curso añadido");
        return false;
      }

      var selected = $("#courseSelect option:selected");
      row = $("<tr>").append(
        $('<td class="bg-slate-100 p-3 border-b border-white">').append(
          $('<input type="hidden" name="schedule[]">').val(scheduleCurrent),
          $('<input type="hidden" name="listCoursesTemp[]" id="listCoursesTemp[]" class="listCoursesTemp">').val(selected.val()),
          " ",
          $('<h3 class="titleCourse text-lg font-bold text-brand-600">').text(selected.text()),
          $('<small class="text-slate-600">').append(
            "Capacidad: ",
            $('<span class="capacity">').text(selected.data("capacity")),
            " Aula: ",
            $('<span class="classRoom">').text(selected.data("classroom")),
            " ",
            $("<br>"),
            "Profesor: ",
            $('<span class="teacher">').text(selected.data("teacher")),
            " ",
            $("<br>"),
            "Horario: ",
            $('<span class="startTime">').text(selected.data("starttime")),
            " - ",
            $('<span class="endTime">').text(selected.data("endtime"))
          )
        ),
        $('<td class="no w-14 cursor-pointer bg-red-600 text-center text-xl text-white hover:bg-red-700 border-b border-white"><i class="fa fa-trash-can"></i></td>')
      );
      $("#courses").append(row);
      $("#courseSelect option:selected").remove();
      updateListCourses();
    } else {
      $("#courseSelect").addClass("error");
      $(".error_course").show();
    }
  });
});

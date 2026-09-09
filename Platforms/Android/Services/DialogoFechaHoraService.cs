using Android.App;
using AndroidX.Fragment.App;
using AView = Android.Views.View;
using G = Google.Android.Material;

namespace MiCachito.Mobile.Platforms.Android.Services;

/// <summary>
/// Diálogos nativos Material de fecha (calendario) y hora (modo teclado),
/// tal como muestran los mockups 7.2 de Depósitos. Fase solo-interfaz.
/// </summary>
public static class DialogoFechaHoraService
{
    /// <summary>Abre el calendario Material y devuelve la fecha elegida (o null si se cancela).</summary>
    public static Task<DateTime?> PickFechaAsync(DateTime inicial)
    {
        var tcs = new TaskCompletionSource<DateTime?>(TaskCreationOptions.RunContinuationsAsynchronously);

        FragmentActivity? activity = Platform.CurrentActivity as FragmentActivity;
        if (activity is null || activity.IsFinishing || activity.IsDestroyed)
        {
            tcs.TrySetResult(null);
            return tcs.Task;
        }

        long utcMillis = new DateTimeOffset(DateTime.SpecifyKind(inicial, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

        G.DatePicker.MaterialDatePicker.Builder builder = G.DatePicker.MaterialDatePicker.Builder.DatePicker();
        builder.SetSelection(Java.Lang.Long.ValueOf(utcMillis));
        G.DatePicker.MaterialDatePicker picker = builder.Build();

        picker.AddOnPositiveButtonClickListener(
            new MiPickerClickListener(() =>
            {
                Java.Lang.Object? sel = picker.Selection;
                if (sel is Java.Lang.Long l)
                {
                    DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds(l.LongValue());
                    tcs.TrySetResult(dto.UtcDateTime);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            }));

        picker.Show(activity.SupportFragmentManager, "deposito_fecha");
        return tcs.Task;
    }

    /// <summary>Abre el teclado horario Material (12h AM/PM) y devuelve la hora elegida (o null si se cancela).</summary>
    public static Task<TimeSpan?> PickHoraAsync(TimeSpan inicial)
    {
        var tcs = new TaskCompletionSource<TimeSpan?>(TaskCreationOptions.RunContinuationsAsynchronously);

        FragmentActivity? activity = Platform.CurrentActivity as FragmentActivity;
        if (activity is null || activity.IsFinishing || activity.IsDestroyed)
        {
            tcs.TrySetResult(null);
            return tcs.Task;
        }

        G.TimePicker.MaterialTimePicker.Builder builder = new();
        builder.SetHour(inicial.Hours);
        builder.SetMinute(inicial.Minutes);
        builder.SetTimeFormat(G.TimePicker.TimeFormat.Clock12h);
        builder.SetInputMode(G.TimePicker.MaterialTimePicker.InputModeKeyboard);
        G.TimePicker.MaterialTimePicker picker = builder.Build();

        picker.AddOnPositiveButtonClickListener(
            new MiViewClickListener(() =>
            {
                int hora = picker.Hour;
                int minuto = picker.Minute;
                tcs.TrySetResult(new TimeSpan(hora, minuto, 0));
            }));

        picker.Show(activity.SupportFragmentManager, "deposito_hora");
        return tcs.Task;
    }

    /// <summary>Listener del MaterialDatePicker (Acepta: interfaz Material del paquete DatePicker).</summary>
    private sealed class MiPickerClickListener : Java.Lang.Object,
        Google.Android.Material.DatePicker.IMaterialPickerOnPositiveButtonClickListener
    {
        private readonly Action _accion;

        public MiPickerClickListener(Action accion) => _accion = accion;

        public void OnPositiveButtonClick(Java.Lang.Object? selection)
        {
            // La selección se lee del picker dentro del callback; solo sirve de puente.
            _accion();
        }
    }

    /// <summary>Listener del MaterialTimePicker (Acepta: OnClickListener estándar de View).</summary>
    private sealed class MiViewClickListener : Java.Lang.Object, AView.IOnClickListener
    {
        private readonly Action _accion;

        public MiViewClickListener(Action accion) => _accion = accion;

        public void OnClick(AView? v) => _accion();
    }
}

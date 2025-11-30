import serial
import os
import csv
import datetime
import threading
import socket
import tkinter as tk
from tkinter import messagebox
from tkinter import ttk
import json
from tkinter import filedialog

# -------- CONFIGURACIÓN SERIAL --------
BAUD_RATE = 9600
DATA_BITS = serial.SEVENBITS
PARITY = serial.PARITY_NONE
STOP_BITS = serial.STOPBITS_ONE

# -------- VARIABLES --------
running = True
PORT = ""
csv_path = ""
log_path = ""
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
UDP_IP = "127.0.0.1"
UDP_PORT = 5005

commands = []
command_thread = None
command_index = 0
session_start_time = None
current_command_label = None
paused = False

wait_for_event = None
event_received = threading.Event()


# -------- FUNCIONES DE UTILIDAD --------
def current_timestamp():
    return datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")

def short_time():
    return datetime.datetime.now().strftime("%H:%M:%S")

def checksum(packet):
    chk_sum = sum(packet[1:-1]) & 0xFF
    return (~chk_sum) & 0xFF == packet[-1]

def send_message(key, value):
    msg = f"{key},{value}"
    sock.sendto(msg.encode(), (UDP_IP, UDP_PORT))
    print("Message sent:", msg)

def parse_packet(packet):
    poor_signal, attention, meditation = 0, 0, 0
    eeg_values = [0] * 8

    if not checksum(packet):
        print("Checksum inválido")
        return None

    i = 1
    while i < len(packet) - 1:
        code = packet[i]
        if code == 0x02:
            poor_signal = packet[i + 1]
            i += 2
        elif code == 0x04:
            attention = packet[i + 1]
            i += 2
        elif code == 0x05:
            meditation = packet[i + 1]
            i += 2
        elif code == 0x83:
            eeg_data = packet[i+2:i+26+2]
            for j in range(8):
                start = j * 3
                value = (eeg_data[start] << 16) | (eeg_data[start+1] << 8) | eeg_data[start+2]
                eeg_values[j] = value
            i += 26
        else:
            i += 1

    return poor_signal, attention, meditation, eeg_values

def read_serial():
    global running, PORT
    try:
        ser = serial.Serial(PORT, BAUD_RATE, bytesize=DATA_BITS, parity=PARITY, stopbits=STOP_BITS, timeout=1)
        print("Conectado al puerto serie:", PORT)
    except Exception as e:
        print("No se pudo conectar al puerto:", e)
        running = False
        return

    buffer = []
    last_byte = None
    in_packet = False

    while running:
        try:
            byte = ser.read(1)
            if not byte:
                continue

            latest_byte = byte[0]

            if last_byte == 0xAA and latest_byte == 0xAA:
                buffer = []
                in_packet = True
            elif in_packet:
                buffer.append(latest_byte)
                if len(buffer) > 0 and len(buffer) == buffer[0] + 2:
                    result = parse_packet(buffer)
                    if result:
                        poor_signal, attention, meditation, eeg_values = result
                        timestamp = current_timestamp()

                        # GUARDAR EN CSV
                        with open(csv_path, mode='a', newline='') as file:
                            writer = csv.writer(file)
                            send_message("ATT", attention)
                            writer.writerow([timestamp, poor_signal, attention, meditation] + eeg_values)

                        # ACTUALIZAR INTERFAZ
                        root.after(0, update_gui_values, poor_signal, attention, meditation)

                    buffer = []
                    in_packet = False

            last_byte = latest_byte
        except Exception as e:
            print("Error de lectura:", e)
            break

    ser.close()
    print("Puerto serie cerrado.")

def toggle_pause():
    global paused
    paused = not paused
    state = "Pause" if paused else "Resume"

    # Log
    append_log(f"{state.lower()} by admin.")

    # Actualizar botón
    pause_button.config(text="Reanudar" if paused else "Pausar")

    # Enviar mensaje por socket al juego
    if paused:
        send_message("PAUSE_GAME", "")
    else:
        send_message("RESUME_GAME", "")

def append_log(msg):
    timestamp = current_timestamp()
    with open(log_path, mode='a') as file:
        file.write(f"[{timestamp}] {msg}\n")
    print(f"Log guardado: [{timestamp}] {msg}")

def load_json_commands():
    global commands, command_index

    file_path = filedialog.askopenfilename(filetypes=[("JSON Files", "*.json")])
    if not file_path:
        return

    try:
        with open(file_path, 'r') as f:
            commands = json.load(f)
        command_index = 0
        append_log(f"Archivo de comandos cargado: {file_path}")
        messagebox.showinfo("Cargado", f"{len(commands)} comandos cargados.")
    except Exception as e:
        messagebox.showerror("Error", f"No se pudo cargar el archivo: {e}")

def run_command_sequence():
    global command_index, session_start_time, wait_for_event, event_received

    session_start_time = datetime.datetime.now()

    while running and command_index < len(commands):
        if paused:
            threading.Event().wait(0.1)
            continue

        next_command = commands[command_index]

        if "comment" in next_command:
            append_log(f"Info: {next_command["comment"]}")
            command_index += 1
            continue

        # Caso especial: esperar evento externo
        if "wait_for" in next_command:
            wait_for_event = next_command["wait_for"]
            append_log(f"Waiting: {wait_for_event}")

            # Actualizo UI
            if current_command_label:
                current_command_label.config(text=f"Esperando {wait_for_event}")

            # Bloqueo hasta que llegue evento de Unity o botón manual
            event_received.clear()
            event_received.wait()  

            append_log(f"Received: {wait_for_event}")
            wait_for_event = None

            # Reiniciar tiempo de referencia para evitar solapamientos
            session_start_time = datetime.datetime.now()
            command_index += 1
            continue

        # Comandos temporales normales
        now = datetime.datetime.now()
        elapsed = (now - session_start_time).total_seconds()

        if elapsed >= next_command['time']:
            key = next_command['command']
            value = next_command['value']
            send_message(key, value)
            append_log(f"Sent: {key}={value} (t+{elapsed:.2f}s)")
            if current_command_label:
                current_command_label.config(text=f"Último: {key} = {value}")
            command_index += 1
        else:
            threading.Event().wait(0.1)
            
def start_command_sequence():
    global command_thread

    if not commands:
        messagebox.showwarning("No hay comandos", "Primero carga un archivo JSON.")
        return

    command_thread = threading.Thread(target=run_command_sequence)
    command_thread.start()
    append_log("COMMAND SEQUENCE STARTED")

# -------- FUNCIONES GUI --------
def update_gui_values(poor_signal, attention, meditation):
    attention_var.set(attention)
    meditation_var.set(meditation)

    if poor_signal == 0:
        signal_status.config(fg="green")
        signal_var.set("Excelente (0)")
    elif poor_signal < 50:
        signal_status.config(fg="orange")
        signal_var.set(f"Buena ({poor_signal})")
    else:
        signal_status.config(fg="red")
        signal_var.set(f"Mala ({poor_signal})")

def start_session():
    global PORT, csv_path, log_path, running

    PORT = port_entry.get().strip()
    paciente = paciente_entry.get().strip()
    actividad = actividad_entry.get().strip()

    if not (PORT and paciente and actividad):
        messagebox.showerror("Error", "Por favor completa todos los campos.")
        return

    base_folder = os.path.join(os.getcwd(), "data", paciente, actividad)
    os.makedirs(base_folder, exist_ok=True)

    csv_path = os.path.join(base_folder, "datos.csv")
    log_path = os.path.join(base_folder, "log.txt")

    headers = ["Timestamp", "PoorSignal", "Attention", "Meditation",
               "Delta", "Theta", "LowAlpha", "HighAlpha", "LowBeta", "HighBeta", "LowGamma", "MidGamma"]

    with open(csv_path, mode='w', newline='') as file:
        writer = csv.writer(file)
        writer.writerow(headers)

    with open(log_path, mode='w') as file:
        file.write(f"Session Log\nFecha de inicio: {datetime.datetime.now()}\n")

    running = True
    thread_serial = threading.Thread(target=read_serial)
    thread_serial.start()

    thread_unity = threading.Thread(target=listen_for_unity_events, daemon=True)
    thread_unity.start()

    messagebox.showinfo("Conectando al dispositivo", "La grabación comenzará en breves...")

def send_log_from_gui():
    msg = log_entry.get().strip()
    if msg:
        append_log(msg)
        log_entry.delete(0, tk.END)

def send_socket_from_gui():
    key = key_entry.get().strip()
    value = value_entry.get().strip()
    if key and value:
        append_log(f"{key}, {value}")
        send_message(key, value)
        key_entry.delete(0, tk.END)
        value_entry.delete(0, tk.END)

def update_clock():
    clock_label.config(text=short_time())
    root.after(1000, update_clock)

def manual_end_action():
    """Botón en GUI para desbloquear un wait_for manualmente."""
    if wait_for_event:
        append_log(f"ENDED MANUALLY: {wait_for_event}")
        event_received.set()

# -------- INTERFAZ TKINTER --------
root = tk.Tk()
root.title("Admin EEG")

# Tkinter reference to update progress bars
attention_var = tk.IntVar()
meditation_var = tk.IntVar()
signal_var = tk.StringVar(value="---")

# Reloj
clock_label = tk.Label(root, font=("Courier", 18), fg="blue")
clock_label.pack(pady=5)
update_clock()

# Configuración de sesión
tk.Label(root, text="Puerto COM (ej: COM5 o /dev/ttyUSB0):").pack()
port_entry = tk.Entry(root, width=30)
port_entry.pack()

tk.Label(root, text="Nombre del paciente:").pack()
paciente_entry = tk.Entry(root, width=30)
paciente_entry.pack()

tk.Label(root, text="Descripción de la actividad:").pack()
actividad_entry = tk.Entry(root, width=30)
actividad_entry.pack()

tk.Button(root, text="Iniciar sesión", command=start_session).pack(pady=10)

# Visualización en tiempo real
tk.Label(root, text="Nivel de atención:").pack()
attention_bar = ttk.Progressbar(root, orient="horizontal", length=300, mode="determinate", variable=attention_var, maximum=100)
attention_bar.pack()

tk.Label(root, text="Nivel de meditación:").pack()
meditation_bar = ttk.Progressbar(root, orient="horizontal", length=300, mode="determinate", variable=meditation_var, maximum=100)
meditation_bar.pack()

tk.Label(root, text="Calidad de señal:").pack()
signal_status = tk.Label(root, textvariable=signal_var, font=("Arial", 12, "bold"))
signal_status.pack()

# Campo log manual
tk.Label(root, text="Mensaje al log:").pack()
log_entry = tk.Entry(root, width=40)
log_entry.pack()
tk.Button(root, text="Enviar al log", command=send_log_from_gui).pack(pady=5)

# Campo socket manual
tk.Label(root, text="Enviar por socket (clave + valor):").pack()
key_entry = tk.Entry(root, width=20)
key_entry.pack()
value_entry = tk.Entry(root, width=20)
value_entry.pack()
tk.Button(root, text="Enviar por socket", command=send_socket_from_gui).pack(pady=5)

tk.Button(root, text="Cargar JSON de comandos", command=load_json_commands).pack(pady=5)
tk.Button(root, text="Iniciar secuencia de comandos", command=start_command_sequence).pack(pady=5)
tk.Button(root, text="Forzar END_ACTION", command=manual_end_action).pack(pady=5)

pause_button = tk.Button(root, text="Pausar", command=toggle_pause)
pause_button.pack(pady=5)

current_command_label = tk.Label(root, text="Último: ---")
current_command_label.pack(pady=5)

def listen_for_unity_events():
    """Escucha en UDP_PORT+1 lo que envía Unity y lo registra en el log."""
    listener_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    listener_socket.bind((UDP_IP, UDP_PORT + 1))
    print(f"Escuchando mensajes desde Unity en {UDP_IP}:{UDP_PORT+1}")

    while running:
        try:
            data, _ = listener_socket.recvfrom(1024)
            message = data.decode().strip()
            append_log(f"Received: {message}")

            if wait_for_event and message == wait_for_event:
                event_received.set()
        except Exception as e:
            print("Error en socket de escucha:", e)

root.mainloop()

import os
import csv
from datetime import datetime, timedelta

def parse_timestamp_log(line):
    """Extrae el timestamp de una línea del log."""
    try:
        ts_str = line.split(']')[0].strip('[')
        return datetime.strptime(ts_str, "%Y-%m-%d %H:%M:%S.%f")
    except Exception:
        return None

def parse_timestamp_data(ts_str):
    """Convierte timestamp de data_clean.csv a datetime (solo hasta segundos)."""
    try:
        # Ejemplo: 2025-11-06 15:36:54.6
        return datetime.strptime(ts_str.split('.')[0], "%Y-%m-%d %H:%M:%S")
    except Exception:
        return None

def find_nearest_time(target, times):
    """Encuentra el timestamp más cercano a target en una lista."""
    return min(times, key=lambda t: abs(t - target))

def process_directory(directory):
    log_path = os.path.join(directory, "log.txt")
    data_path = os.path.join(directory, "data_clean.csv")

    if not os.path.exists(log_path) or not os.path.exists(data_path):
        return

    sections_dir = os.path.join(directory, "sections_data")
    os.makedirs(sections_dir, exist_ok=True)

    # --- Leer data_clean.csv ---
    data = []
    with open(data_path, "r", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            ts = parse_timestamp_data(row["Timestamp"])
            if ts:
                data.append((ts, row["Attention"]))
    data.sort(key=lambda x: x[0])

    # --- Leer log.txt y detectar Info ---
    info_entries = []
    with open(log_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
        for i, line in enumerate(lines):
            if "Info:" in line:
                ts = parse_timestamp_log(line)
                if ts:
                    info_text = line.split("Info:")[1].strip()
                    info_entries.append((ts, info_text, i))

    # --- Procesar bloques entre Infos ---
    counts = {}  # para evitar duplicados
    for idx, (start_ts, info_name, start_line_idx) in enumerate(info_entries):
        if "descanso" in info_name:
            continue  # ignorar descansos

        # Determinar fin del bloque
        if idx + 1 < len(info_entries):
            end_ts = info_entries[idx + 1][0]
            end_line_idx = info_entries[idx + 1][2]
        else:
            # Si es el último bloque, hasta el final del archivo
            end_ts = data[-1][0] if data else start_ts
            end_line_idx = len(lines)

        # Crear nombre único
        clean_name = info_name.replace(" ", "_")
        count = counts.get(clean_name, 0)
        folder_name = clean_name if count == 0 else f"{clean_name}_{count}"
        counts[clean_name] = count + 1

        section_dir = os.path.join(sections_dir, folder_name)
        os.makedirs(section_dir, exist_ok=True)

        # --- Guardar fragmento de log ---
        section_log_path = os.path.join(section_dir, "log.txt")
        with open(section_log_path, "w", encoding="utf-8") as lf:
            lf.writelines(lines[start_line_idx:end_line_idx])

        # --- Guardar fragmento de data_clean.csv ---
        margin = timedelta(seconds=1)
        start_ts -= margin
        end_ts += margin

        filtered = [(ts, att) for ts, att in data if start_ts <= ts <= end_ts]
        if not filtered and data:
            # si no hay coincidencias, usar los más cercanos
            nearest_start = find_nearest_time(start_ts, [d[0] for d in data])
            nearest_end = find_nearest_time(end_ts, [d[0] for d in data])
            filtered = [(ts, att) for ts, att in data if nearest_start <= ts <= nearest_end]

        section_data_path = os.path.join(section_dir, "data.csv")
        with open(section_data_path, "w", newline='', encoding="utf-8") as cf:
            writer = csv.writer(cf)
            writer.writerow(["Timestamp", "Attention"])
            for ts, att in filtered:
                writer.writerow([ts.strftime("%Y-%m-%d %H:%M:%S"), att])

        print(f"✅ Creado bloque: {folder_name} ({len(filtered)} filas)")

def main():
    base_dir = os.getcwd()
    for root, dirs, files in os.walk(base_dir):
        if "data_clean.csv" in files and "log.txt" in files:
            print(f"Procesando: {root}")
            process_directory(root)

if __name__ == "__main__":
    main()

import os
import csv
from datetime import datetime

# Recorre todas las carpetas y subcarpetas desde el directorio actual
for root, dirs, files in os.walk("."):
    if "datos.csv" in files:
        input_path = os.path.join(root, "datos.csv")
        output_path = os.path.join(root, "data_clean.csv")

        try:
            with open(input_path, newline="", encoding="utf-8") as infile:
                reader = csv.DictReader(infile)
                fieldnames = ["Timestamp", "Attention"]

                # Crear el nuevo archivo limpio
                with open(output_path, "w", newline="", encoding="utf-8") as outfile:
                    writer = csv.DictWriter(outfile, fieldnames=fieldnames)
                    writer.writeheader()

                    for row in reader:
                        if "Timestamp" in row and "Attention" in row:
                            try:
                                # Formatear el Timestamp
                                dt = datetime.strptime(row["Timestamp"], "%Y-%m-%d %H:%M:%S.%f")
                                timestamp_str = dt.strftime("%Y-%m-%d %H:%M:%S.%f")[:-5]  # un decimal

                                writer.writerow({
                                    "Timestamp": timestamp_str,
                                    "Attention": row["Attention"]
                                })
                            except Exception:
                                # Si alguna fila está mal formada, se salta
                                continue

            print(f"✅ Creado: {output_path}")

        except Exception as e:
            print(f"❌ Error procesando {input_path}: {e}")

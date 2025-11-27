import os
import shutil

# Directorio base desde donde empezar a buscar
base_dir = "./"  # <- Cambiar por tu ruta
all_data_dir = os.path.join(base_dir, "all_data")

# Crear carpeta all_data si no existe
os.makedirs(all_data_dir, exist_ok=True)

for root, dirs, files in os.walk(base_dir):
    if os.path.basename(root) == "sections_data":
        # Buscar dentro de cada subcarpeta de sections_data
        for section_folder in dirs:
            section_path = os.path.join(root, section_folder)
            for sub_root, _, sub_files in os.walk(section_path):
                csv_files = [f for f in sub_files if f.endswith(".csv")]
                if not csv_files:
                    continue

                # Crear carpeta en all_data con el nombre del sub_root
                folder_name = os.path.basename(sub_root)
                dst_folder = os.path.join(all_data_dir, folder_name)
                os.makedirs(dst_folder, exist_ok=True)

                for file in csv_files:
                    # Construir nuevo nombre
                    carpeta_padre = os.path.basename(os.path.dirname(os.path.dirname(os.path.dirname(section_path))))
                    subcarpeta = os.path.basename(os.path.dirname(os.path.dirname(section_path)))
                    carpeta_csv = os.path.basename(sub_root)
                    new_name = f"{carpeta_padre}_{subcarpeta}_{carpeta_csv}.csv"

                    # Copiar CSV a la carpeta correspondiente en all_data
                    src_path = os.path.join(sub_root, file)
                    dst_path = os.path.join(dst_folder, new_name)
                    shutil.copy2(src_path, dst_path)
                    print(f"Copiado: {src_path} -> {dst_path}")

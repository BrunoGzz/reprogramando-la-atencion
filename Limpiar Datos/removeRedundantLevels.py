import os
import shutil

# Carpetas a eliminar
folders_to_remove = {
    "esperar_usuario",
    "juego_principal_con_punto_extendido",
    "juego_principal_sin_punto_extendido"
}

def remove_folders(base_dir):
    sections_dir = os.path.join(base_dir, "sections_data")
    if not os.path.exists(sections_dir):
        return

    for entry in os.listdir(sections_dir):
        path = os.path.join(sections_dir, entry)
        if os.path.isdir(path) and entry in folders_to_remove:
            shutil.rmtree(path)
            print(f"🗑️ Eliminada carpeta: {path}")

def main():
    base_dir = os.getcwd()
    for root, dirs, files in os.walk(base_dir):
        if "sections_data" in dirs:
            remove_folders(root)

if __name__ == "__main__":
    main()

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace SMTDatabase
{
    class LoteHandle
    {
        public string bom = "";
        public string descripcion = "";
        public string lote_version = "";
        public string item_num = "";
        public string logop = "";
        public string posicion = "";
        public string componente = "";
        public string descripcion_componente = "";       
        public string cantidad = "";
        public string unidad_medida = "";
        public string asignacion = "";
        public string fecha = "";
        public string subinventario = "";
        public string localizador = "";
        public string tipo_material = "";
        public string kit = "";
        public string placa = "";
        public string sustituto = "";
        public string item_cygnus = "";
        public string item_type= "";

        public static char confSeparador = '\t';

        public static string[] columnas = { 
            "bom",
            "descripcion",
            "lote_version",
            "item_num",
            "logop",
            "posicion",
            "componente",
            "descripcion_componente",
            "cantidad",
            "unidad_medida",
            "asignacion",
            "fecha",
            "subinventario",
            "localizador",
            "tipo_material",
            "kit",
            "placa",
            "sustituto",
            "item_cygnus",
            "item_type",
        };

        public static string[] columnas_new = { 
            "bom",
            "descripcion",
            "lote_version",
            "item_num",
            "logop",
            "posicion",
            "componente",
            "descripcion_componente",
            "cantidad",
            "unidad_medida",
            "asignacion",
            "fecha",
            "tiposuministro",
            "subinventario",
            "localizador",
            "tipo_material",
            "kit",
            "placa",
            "sustituto",
            "item_cygnus",
            "item_type",
        };
//        public static int[] indexado = null; //{ 4, 5, 6, 7, 10 }; 

        public static DataTable read(string file)
        {
            DataTable dt = new DataTable(); // Creo una Datatable nueva.

            try
            {
                StreamReader reader = new StreamReader(file);
                string contenido = reader.ReadToEnd();
                reader.Close();

                // Separo las lineas por filas.
                string[] lineas = contenido.Split('\n'); 

                bool first = true;
                foreach (string linea in lineas)
                {
                    // Separo por columnas
                    string[] rows = linea.Split(confSeparador);
                    
                    // Me aseguro que las filas contengan mas de 10 columnas.
                    if (rows.Length > 10)
                    {
                        // Si es la primer fila, la ingreso como HEADERs
                        if (first) 
                        {
                            if (linea.ToLower().Contains("suministro"))
                            {
                                columnas = columnas_new;
                            }
                            foreach(string col in columnas) {
                                dt.Columns.Add(col);
                            }
                            first = false;
                        }
                        else 
                        {
                            dt.Rows.Add(rows.ToArray());
                        }
                    }
                }
                return dt;
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
                return dt;
            }
        }
    }
}

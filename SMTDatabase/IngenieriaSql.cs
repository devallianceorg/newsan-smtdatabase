using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace SMTDatabase
{
    class IngenieriaSql
    {
        // Obtengo hash de MySQL
        public static int add(string modelo, string lote, string hash, string fecha_modificacion,string version)
        {
            int id = 0;
            Mysql sql = new Mysql();

            string query = "INSERT INTO `ingenieria` (`id`, `modelo`, `lote`, `hash`,`fecha_modificacion`,`version`) VALUES (NULL, '" + modelo + "', '" + lote + "', '" + hash + "', '" + fecha_modificacion + "', '" + version+ "');";
            bool rs = sql.Ejecutar(query);
            if (rs)
            {
                // Si fue agregado obtendo ID.
                DataTable dt = sql.Select("select id from ingenieria where modelo = '" + modelo + "' and lote = '" + lote + "' and hash = '" + hash + "' and fecha_modificacion = '" + fecha_modificacion + "' limit 1");
                if (sql.rows)
                {
                    DataRow r = dt.Rows[0];
                    id = int.Parse(r["id"].ToString());
                }
            }
            // Retorno ID, 0 si no pudo insertar.
            return id;
        }

        // Borrar Lote
        public static bool del(int id)
        {
            Mysql sql = new Mysql();
            string query = "delete from `ingenieria` where id = '" + id+ "' limit 1";
            bool rs = sql.Ejecutar(query);
            if (rs)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        // Agrego lista de ingenieria del lote al SQL
        public static bool addIngenieria(DataTable loteinfo, int id_ingenieria, string version)
        {
            Mysql sql = new Mysql();
            string query = "INSERT INTO `lotes` (`id`, `id_ingenieria`, `id_ver`, `bom`, `descripcion`, `lote_version`, `item_num`, `logop`, `posicion`, `componente`, `descripcion_componente`, `cantidad`, `unidad_medida`, `asignacion`, `fecha`, `subinventario`, `localizador`, `tipo_material`, `kit`, `placa`, `sustituto`, `item_cygnus`, `item_type`) VALUES ";
            bool rs = false;
            
            List<string> values = new List<string>();

            if (loteinfo.Rows.Count > 0)
            {
                foreach (DataRow r in loteinfo.Rows)
                {
                    string bom = Slashes(r["bom"].ToString());
                    string descripcion = Slashes(r["descripcion"].ToString());
                    string lote_version = Slashes(r["lote_version"].ToString());
                    string item_num = Slashes(r["item_num"].ToString());
                    string logop = Slashes(r["logop"].ToString());
                    string posicion = Slashes(r["posicion"].ToString());
                    string componente = Slashes(r["componente"].ToString());
                    string descripcion_componente = Slashes(r["descripcion_componente"].ToString());
                    string cantidad = Slashes(r["cantidad"].ToString());
                    string unidad_medida = Slashes(r["unidad_medida"].ToString());
                    string asignacion = Slashes(r["asignacion"].ToString());
                    string fecha = Slashes(r["fecha"].ToString());
                    string subinventario = Slashes(r["subinventario"].ToString());
                    string localizador = Slashes(r["localizador"].ToString());
                    string tipo_material = Slashes(r["tipo_material"].ToString());
                    string kit = Slashes(r["kit"].ToString());
                    string placa = Slashes(r["placa"].ToString());
                    string sustituto = Slashes(r["sustituto"].ToString());
                    string item_cygnus = Slashes(r["item_cygnus"].ToString());
                    string item_type = Slashes(r["item_type"].ToString());

                    values.Add("  (NULL, " + id_ingenieria + ", " + version + ", '" + bom + "', '" + descripcion + "', '" + lote_version + "','" + item_num + "','" + logop + "','" + posicion + "','" + componente + "','" + descripcion_componente + "','" + cantidad + "','" + unidad_medida + "','" + asignacion + "','" + fecha + "','" + subinventario + "','" + localizador + "','" + tipo_material + "','" + kit + "','" + placa + "','" + sustituto + "','" + item_cygnus + "','" + item_type + "') ");
                }

                query = query + string.Join(",", values.ToArray()) + ";";
                rs = sql.Ejecutar(query);
            }
            // "query" contiene toda la lista de ingenieria en un unico INSERT. 
            return rs;
        }

        // Actualizo ultima fecha de modificacion
        public static void updateFecha(int id, string fecha_modificacion)
        {
            Mysql sql = new Mysql();
            sql.Ejecutar("update ingenieria set fecha_modificacion = '" + fecha_modificacion + "' where id = '" + id + "' limit 1");
        } 

        // Reemplaza caracteres especiales 
        public static string Slashes(string InputTxt)
        {
            // List of characters handled:
            // \000 null
            // \010 backspace
            // \011 horizontal tab
            // \012 new line
            // \015 carriage return
            // \032 substitute
            // \042 double quote
            // \047 single quote
            // \134 backslash
            // \140 grave accent

            string Result = InputTxt;

            try
            {
                Result = System.Text.RegularExpressions.Regex.Replace(InputTxt, @"[\042\047\134]", "\\$0");
            }
            catch (Exception Ex)
            {
                // handle any exception here
            }

            return Result;
        }

        // Lee archivo y devuelve el contenido en un string
        public static string ReadFile(string file)
        {
            string content = "";

            if (File.Exists(file))
            {
                StreamReader reader = new StreamReader(file);
                content = reader.ReadToEnd();
                reader.Close();
            }

            return content;
        }

        // Lectura de archivo y devolucion en datatable
        public static DataTable FileToTable(string file, char SEPARADOR = '\t', bool INDEXADO = true, string[] COLUMNAS = null)
        {
            char FILAS = '\n';

            DataTable dt = new DataTable();
            //            try
            //            {
            string content = ReadFile(file);
            string[] lineas = content.Split(FILAS); // Separo las lineas por filas.

            // Primer columna como header?
            bool first = true;
            bool customHeaderDone = false;

            bool customHeader = false;
            if (COLUMNAS != null && (COLUMNAS.Length > 0))
            {
                customHeader = true;
            }

            foreach (string linea in lineas)
            {
                string[] rows = linea.Split(SEPARADOR);

                if (rows.Length > 1) // Me aseguro que las filas contengan mas de una columna.
                {
                    if (first && !customHeader) // Si es la primer fila, y no defino columnas por defecto, ingreso la linea como columna.
                    {
                        int j = 0;
                        foreach (string row in rows)
                        {
                            if (INDEXADO) // Puedo asignar columnas con su respectivo INDEX.
                            {
                                dt.Columns.Add(j.ToString());
                            }
                            else
                            {
                                if (dt.Columns.Contains(row.ToLower()))
                                {
                                    dt.Columns.Add(j + "_" + row.ToLower());
                                }
                                else
                                {
                                    dt.Columns.Add(row.ToLower());
                                }
                            }
                            j++;
                        }
                        first = false;
                    }
                    else
                    {
                        if (customHeader)
                        {
                            if (!customHeaderDone) // Si existen columnas personalizadas, y estas no se han seteado
                            {
                                for (int i = 0; i < COLUMNAS.Length; i++) // Cargo columnas personalizadas.
                                {
                                    dt.Columns.Add(COLUMNAS[i].ToString());
                                }
                                customHeaderDone = true;
                            }
                        }
                        dt.Rows.Add(rows); // Agrego filas.
                    }
                }
            }
            //           }
            //            catch (Exception e)
            //            {
            //return dt;
            //            }

            return dt;
        }

        // Obtiene una linea especifica del contenido de un archivo
        public static string GetLine(string fileContent, int line)
        {
            string return_line = "";
            string[] lines = fileContent.Split('\n');
            return_line = lines[line].Replace('\r', ' ');
            return return_line;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Data;

namespace SMTDatabase
{
    class Hash
    {
        public static List<Hash> Database = new List<Hash>();

        public int id = 0;
        public string modelo = "";
        public string lote = "";
        public string hash = "";
        public string fecha_modificacion = "";
        public string version = "";

        public bool existe = false;
        public bool modificado = false;

        // Obtengo hash de MySQL
        public static void getFromSql()
        {
            // Reseteo resultados SQL anteriores.
            Database = new List<Hash>();

            Mysql sql = new Mysql();
            DataTable query = sql.Select("select id,modelo,lote,hash,DATE_FORMAT(fecha_modificacion,'%Y-%m-%d %H:%i:%s') as fecha_modificacion,version from ingenieria");
            if (sql.rows && !sql.mysql_error)
            {
                foreach (DataRow r in query.Rows)
                {
                    addHashList(
                        int.Parse(r["id"].ToString()),
                        r["modelo"].ToString(),
                        r["lote"].ToString(),
                        r["hash"].ToString(),
                        r["fecha_modificacion"].ToString(),
                        r["version"].ToString()
                    );
                }
            }
        }

        // Genero una lista de hash        
        public static void addHashList(int id, string modelo, string lote, string hash, string fecha_modificacion, string version)
        {
            Hash n = new Hash();
            n.id = id;
            n.modelo = modelo;
            n.lote = lote;
            n.hash = hash;
            n.fecha_modificacion = fecha_modificacion;
            n.version = version;

            Database.Add(n);
        }

        public static string generar(string filePath)
        {
            var sBuilder = new StringBuilder();
            try
            {
                byte[] computedHash = new MD5CryptoServiceProvider().ComputeHash(File.ReadAllBytes(filePath));
                foreach (byte b in computedHash)
                {
                    sBuilder.Append(b.ToString("x2").ToLower());
                }
            } catch (Exception e) {
            }
            return sBuilder.ToString();
        }

    }
}

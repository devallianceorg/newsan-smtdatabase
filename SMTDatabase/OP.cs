using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;

namespace SMTDatabase
{
    class OP
    {
        public string numero_op = "";
        public string lote = "";
        public string panel = "";
        public string modelo = "";
        public string qty = "";

        public static List<OP> DBOPList;

        public static void DownloadAll() {
            string query = "select op,qty from `smtdatabase`.`orden_trabajo`";
            Mysql sql = new Mysql();
            DataTable dt = sql.Select(query);
            List<OP> op_list = new List<OP>();

            if (sql.rows)
            {
                foreach(DataRow r in dt.Rows) {
                    OP theop = new OP();
                    theop.numero_op = r["op"].ToString();
                    theop.qty = r["qty"].ToString();
                    op_list.Add(theop);
                }
            }
            DBOPList = op_list;
        }

        public static string getQty(FileInfo file) {
            string qty = "0";

            try
            {
                DataTable opfile = IngenieriaSql.FileToTable(file.FullName);
                DataView dv = opfile.DefaultView;
                dv.Sort = "3 desc";

                opfile = dv.ToTable();

                DataRow dr = opfile.Rows[0];

                double unity = double.Parse(dr.ItemArray[3].ToString());
                double require = double.Parse(dr.ItemArray[6].ToString());

                qty = (require / unity).ToString();
            }
            catch (Exception ex)
            {
                qty = "0";
            }
            return qty;
        }
        
        public static List<OP> FolderOP(DirectoryInfo modelo)
        {
            List<OP> op = new List<OP>();

            DirectoryInfo[] all_pcb = modelo.GetDirectories();
            /*
             * Lista las carpetas del modelo ej: MAIN,CTRL,KEY,SIR
             * Una vez listadas, busca un archivo OP-XXXXX.txt
             * y genera un objeto OP.
             */ 
            foreach (DirectoryInfo pcb in all_pcb)
            {
                string[] opFiles = Directory.GetFiles(pcb.FullName, "OP-*.txt", SearchOption.AllDirectories);
                if (opFiles.Length > 0)
                {
                    foreach (string result in opFiles)
                    {
                        FileInfo fi = new FileInfo(result);
                        OP opInfo = new OP();
                        opInfo.numero_op = fi.Name.ToUpper().Replace(".TXT", "");
                        opInfo.lote = fi.Directory.Name.ToUpper();
                        opInfo.panel = fi.Directory.Parent.Parent.Name.ToUpper();
                        opInfo.modelo = fi.Directory.Parent.Parent.Parent.Name.ToUpper();
                        opInfo.qty = getQty(fi);

                        if (opInfo.lote.StartsWith("L"))
                        {
                            op.Add(opInfo);
                        }
                    }
                }
            }
            return op;
        }

        public static void Procesar(DirectoryInfo modelo)
        {
            List<OP> folder_op = FolderOP(modelo);

            if (folder_op.Count > 0)
            {
                foreach (OP theOP in folder_op)
                {
                    // Verifico si existe OP en la base de datos.
                    OP existOp = OP.DBOPList.Find(o =>
                        o.numero_op.Equals(theOP.numero_op)
                    );

                    if (existOp == null)
                    {
                        // OP no existe en DB, la agrego.
                        string add = "Modelo:" + theOP.modelo + " - Lote:" + theOP.lote + " - Panel: " + theOP.panel + " - OP:" + theOP.numero_op + " - QTY:" + theOP.qty;

                        Mysql sql = new Mysql();
                        string query = @"
                        INSERT INTO  `smtdatabase`.`orden_trabajo` (
                        `id` ,
                        `op` ,
                        `modelo` ,
                        `lote` ,
                        `panel`,
                        `qty`

                        )
                        VALUES (
                        NULL ,  '" + theOP.numero_op + "',  '" + theOP.modelo + "',  '" + theOP.lote + "', '" + theOP.panel + "',  '" + theOP.qty + @"'
                        );
                        ";
                        sql.Ejecutar(query);
                    }
                    else
                    {
                        if (existOp.qty.Equals("0") && !theOP.qty.Equals("0"))
                        {
                            Mysql sql = new Mysql();
                            string query = @"
                            UPDATE `smtdatabase`.`orden_trabajo` SET 
                            `qty`=  '" + theOP.qty + @"'
                            WHERE `op`= '" + theOP.numero_op + @"';
                            ";
                            sql.Ejecutar(query);

                        }
                    }
                }
            }
        }
    }
}

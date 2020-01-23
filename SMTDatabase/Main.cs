using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace SMTDatabase
{
    public partial class Form1 : Form
    {
        private BackgroundWorker bw = new BackgroundWorker();
        private bool TRABAJANDO = false;

        #region Configuracion_de_inicio

        public Form1()
        {
            InitializeComponent();

            //Muestro la version de la aplicación
            this.Text = AssemblyInfo.appInfo();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Mysql.server = "10.30.10.22";
            Mysql.database = "smtdatabase";
            Mysql.user = "root";
            Mysql.password = "apisql";

            Mysql.errorDebug = false;

            int segundos = 300;

            int tiempo = segundos * 1000;
            timer.Interval = tiempo;

            StartWorker();
            timer.Start();           
        }


        static public bool AlreadyRunning()
        {
            string processName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(processName);
            return (processes.Length > 1);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            StartWorker();
        }

        private void iniciarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartWorker();
        }

        private void StartWorker()
        {
            if (AlreadyRunning())
            {
                MessageBox.Show("Ya esta corriendo!");
                Application.Exit();
            }
            else
            {
                if (!TRABAJANDO)
                {
                    iniciarToolStripMenuItem.Enabled = false;
                    // Reseteo resultados.
                    treeLista.Nodes.Clear();
                    dgvMain.Rows.Clear();

                    // Inicio BackgroundWorker
                    Worker();
                }
            }
        }

        private void Worker()
        {
            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            // INICIO MODULO 
            bw.DoWork += new DoWorkEventHandler(Iniciar);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.RunWorkerAsync();
        }
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TRABAJANDO = false;
            progreso.Value = 0;
            iniciarToolStripMenuItem.Enabled = true;
        }
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progreso.Value = e.ProgressPercentage;
        }
        #endregion

        public int addModeloLote(string lotePath, string modelo, string lote, string hash, string fecha, string version)
        {
            string mensaje = "";
            int id = IngenieriaSql.add(modelo, lote, hash, fecha, version);
            try
            {
                if (!id.Equals("0"))
                {
                    // Si pudo agregar...
                    // Leo contenido del lote
                    DataTable lote_content = LoteHandle.read(lotePath);
                    // Agrego a base de datos.
                    bool ifLoteAdd = IngenieriaSql.addIngenieria(lote_content, id, version);

                    if (ifLoteAdd)
                    {
                        mensaje = "Agregado";
                    }
                    else
                    {
                        mensaje = "ERROR AL IMPORTAR LOTE: " + lote;

                        // Remuevo el lote de SQL
                        IngenieriaSql.del(id);

                        // HardCodeo de fecha... si no importo bien el lote,... el proximo intento se vera.
                        // IngenieriaSql.updateFecha(id, "1985-01-01 10:10:10");
                    }

                    dgvMain.Invoke((MethodInvoker)(() =>
                            dgvMain.Rows.Add(modelo + " " + lote, "", hash, mensaje)
                    ));
                }
            }
            catch (Exception)
            {
                // HardCodeo de fecha... si no importo bien el lote,... el proximo intento se vera.
                IngenieriaSql.updateFecha(id, "1985-01-01 10:10:10");

                dgvMain.Invoke((MethodInvoker)(() =>
                    dgvMain.Rows.Add(modelo + " " + lote, "", hash, "No se agrego: SE ENCUENTRA EN USO")
                ));
            }

            return id;
        }

        public void Iniciar(object sender, DoWorkEventArgs e)
        {
            TRABAJANDO = true;

            // Obtengo lista hash (ultimas modificaciones de LOTES en SQL)
            Hash.getFromSql();
            OP.DownloadAll();

            if (Hash.Database.Count() > 0)
            {
                try
                {
                    // Recorro carpetas de ingenieria
                    DirectoryInfo dir = new DirectoryInfo(@"\\USH-NT-3\v1\Users\INSAUT\PLANTA_3\TECNICOS_3\Programacion\LISTAS");

                    DirectoryInfo[] Folders = dir.GetDirectories();
                    int totalFolder = Folders.Count();
                    int countEstado = 0;
                    progreso.Invoke((MethodInvoker)(() => progreso.Maximum = totalFolder));

                    // Recorro carpetas de modelos
                    foreach (DirectoryInfo modelo in Folders)
                    {
                        OP.Procesar(modelo);
                        // Aumento progreso.
                        countEstado++;

                        // Lista de lotes, solo extencion .txt
                        FileInfo[] lotes = modelo.GetFiles("*.txt");

                        TreeNode idmodelo = null;
                        treeLista.Invoke((MethodInvoker)(() => idmodelo = treeLista.Nodes.Add(modelo.Name)));

                        // Recorro lotes del modelo actual
                        foreach (FileInfo lote in lotes)
                        {
                            string loteNombre = lote.Name.Split('.')[0];

                            // Verifico fecha de modificacion.
                            string lote_fecha_modificacion = lote.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

                            TreeNode idlote = null;

                            treeLista.Invoke((MethodInvoker)(() => idlote = idmodelo.Nodes.Add(loteNombre)));

                            // Verifico si existe el modelo y lote en la base de datos.
                            Hash ModeloLote = Hash.Database.Find(o =>
                                o.modelo.Equals(modelo.Name) &&
                                o.lote.Equals(loteNombre)
                            );
                            string loteHash = "";
                            if (ModeloLote == null)
                            {
                                // Si no existe en SQL....

                                // genero un HASH del lote actual (ultima fecha de modificacion)
                                loteHash = Hash.generar(lote.FullName.ToString());
                                // Agrego a SQL
                                string version = "1";
                                if (!loteHash.Equals(""))
                                {
                                    addModeloLote(lote.FullName, modelo.Name, loteNombre, loteHash, lote_fecha_modificacion, version);
                                }
                            }
                            else
                            {
                                // Verifico si existe en SQL el Modelo/lote
                                int HashListIndex = Hash.Database.FindIndex(o => o.id == ModeloLote.id);
                                if (HashListIndex >= 0)
                                {
                                    // Agrego flag de existencia no solo en SQL sino en carpeta INGENIERIA
                                    Hash hash = Hash.Database[HashListIndex];
                                    hash.existe = true;
                                }

                                if (!ModeloLote.fecha_modificacion.Equals(lote_fecha_modificacion))
                                {
                                    // Si la fecha de modificacion en SQL no es igual a la fecha de modificacion actual.

                                    // Por defecto se detecto una modificacion.

                                    string enuso = "";
                                    string loteModificado = "";

                                    try
                                    {
                                        // genero un HASH del lote actual
                                        loteHash = Hash.generar(lote.FullName.ToString());
                                    }
                                    catch (Exception)
                                    {
                                        enuso = " EN USO ";
                                    }

                                    if (ModeloLote.hash.Equals(loteHash))
                                    {
                                        // El hash es el mismo, no hay modificacion.
                                        loteModificado = "No";

                                        // Actualizo ultima fecha de modificacion en SQL.
                                        IngenieriaSql.updateFecha(ModeloLote.id, lote_fecha_modificacion);

                                        dgvMain.Invoke((MethodInvoker)(() =>
                                            dgvMain.Rows.Add(modelo.Name + " " + loteNombre, loteHash, ModeloLote.hash, loteModificado + " (fecha actualizada)" + enuso)
                                        ));
                                    }
                                    else
                                    {
                                        // En caso de que el HASH sea diferente
                                        loteModificado = "Si, actualizando...";

                                        /*
                                         * NOTA:
                                         * Aca Deberia ELIMINAR la version anterior y guardar la nueva version....
                                         * O podria enviar la version anterior a un LOG y guardar la nueva version....
                                         * Hay que pensarlo... queda pendiente...
                                         */

                                        // Elimino Modelo/Lote + Lote de ingenieria en cascada.
                                        // En la proxima ejecucion se agregara la version nueva.
                                        IngenieriaSql.del(ModeloLote.id);

                                        addModeloLote(lote.FullName, modelo.Name, loteNombre, loteHash, lote_fecha_modificacion, (int.Parse(ModeloLote.version) + 1).ToString());

                                        idlote.BackColor = ColorTranslator.FromHtml("#FF0000");
                                        idmodelo.BackColor = ColorTranslator.FromHtml("#FF0000");

                                        dgvMain.Invoke((MethodInvoker)(() =>
                                            dgvMain.Rows.Add(modelo.Name + " " + loteNombre, loteHash, ModeloLote.hash, loteModificado + enuso)
                                        ));
                                    } // EN IF HASH 
                                } // END IF FECHA_MODIFICACION
                            } // ENF IF MODELO NULL
                        } // END FOREACH LOTE
                        bw.ReportProgress(countEstado);
                    } // END FOREACH MODELO

                    // Listo todos los modelos que NO se hallan listado en INGENIERIA pero si existen en SQL
                    List<Hash> Removidos = Hash.Database.FindAll(o => o.existe == false);
                    foreach (Hash removido in Removidos)
                    {
                        dgvMain.Invoke((MethodInvoker)(() =>
                              dgvMain.Rows.Add(removido.modelo + " " + removido.lote, "", removido.hash, "Removido de ingenieria " + removido.id)
                        ));
                    }
                }
                catch (Exception ex)
                {
                    logBox.Invoke((MethodInvoker)(() => logBox.Items.Add(ex.Message+" | "+DateTime.Now)));
                }
            }
            else
            {
                logBox.Invoke((MethodInvoker)(() => logBox.Items.Add("No se encontraron resultados de HASH en SMTDatabase")));
            }
        }
    }
}

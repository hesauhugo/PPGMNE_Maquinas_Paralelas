using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Gurobi;

namespace MaquinasParalelas
{
    class UPMSV1
    {
        GRBEnv env;
        public GRBModel model;

        GRBVar[,,] X;
        GRBVar[,] C;
        GRBVar[] Cmax;

        public UPMSV1()
        {

            env = new GRBEnv();
            env.Set("LogFile", "mip1.log");
            model = new GRBModel(env);

        }

        public int qtdTarefas;
        public int qtdMaquinas;
        public int[][] tarefasPorMaquinas;
        public int[][,] setupTarefasPorMaquinas;

        public void LerArquivos(string caminho)
        {

            string[] arquivo = File.ReadAllLines(caminho);

            qtdTarefas = int.Parse(arquivo[0].Split()[0]);
            qtdMaquinas = int.Parse(arquivo[0].Split()[2]);
            tarefasPorMaquinas = new int[qtdMaquinas][];
            setupTarefasPorMaquinas = new int[qtdMaquinas][,];

            for (int i = 1; i <= qtdMaquinas; i++)
            {
                int maq = i - 1;
                tarefasPorMaquinas[maq] = new int[qtdTarefas+1];

                for (int j = 2; j < qtdTarefas + 2; j++)
                {

                    int indice = i * 2;

                    tarefasPorMaquinas[maq][j - 1] = int.Parse(arquivo[j].Split('\t')[indice]);

                }

            }

            for (int i = 1; i <= qtdMaquinas; i++)
            {
                int inicioSetup = (qtdTarefas * i) + 3 + i;
                int fimSetup = inicioSetup + qtdTarefas;

                setupTarefasPorMaquinas[i - 1] = new int[qtdTarefas+1, qtdTarefas+1];

                for (int j = inicioSetup; j < fimSetup; j++)
                {

                    string[] setup = arquivo[j].Split('\t');

                    for (int k = 0; k < qtdTarefas; k++)
                    {
                        setupTarefasPorMaquinas[i - 1][j - inicioSetup+1, k+1] = int.Parse(setup[k]);
                    }

                }
            }
        }

        public void CarregaVariaveis()
        {

            X = new GRBVar[qtdMaquinas, qtdTarefas + 1, qtdTarefas + 1];

            for (int i = 0; i < qtdMaquinas; i++)
            {

                for (int j = 0; j < qtdTarefas + 1; j++)
                {

                    for (int k = 1; k < qtdTarefas + 1; k++)
                    {

                        X[i, j, k] = model.AddVar(0, 1, 0, GRB.BINARY, $"X(i,j,k)_{i}_{j}_{k}");

                    }

                }

            }

            C = new GRBVar[qtdMaquinas, qtdTarefas + 1];

            for (int i = 0; i < qtdMaquinas; i++)
            {

                for (int j = 0; j < qtdTarefas+1; j++)
                {

                    C[i, j] = model.AddVar(0, int.MaxValue, 0, GRB.INTEGER, $"C(i,j)_{i}_{j}");

                }

            }

            Cmax = new GRBVar[1];
            Cmax[0] = model.AddVar(0, int.MaxValue, 0, GRB.INTEGER, $"Cmax");
            GRBLinExpr func = new GRBLinExpr();
            func = Cmax[0];
            model.SetObjective(func, GRB.MINIMIZE);

        }

        //ensures that every job is assigned to exactly one machine and has exactly one predecessor.
        public void Restricao01()
        {

            GRBLinExpr expr = new GRBLinExpr();

            for (int k = 1; k < qtdTarefas+1; k++)
            {

                expr.Clear();    

                for (int i = 0; i < qtdMaquinas; i++)
                {

                    for (int j = 0; j < qtdTarefas+1; j++)
                    {

                        if (j != k)
                        {
                            expr.AddTerm(1, X[i, j, k]);
                        }

                    }

                }

                model.AddConstr(expr==1,$"R01_{k}");

            }

        }

        //set the maximum number of successors of every job to one
        public void Restricao02()
        {

            GRBLinExpr expr = new GRBLinExpr();

            for (int j = 1; j < qtdTarefas+1; j++)
            {

                expr.Clear();

                for (int i = 0; i < qtdMaquinas; i++)
                {
                    for (int k = 1; k < qtdTarefas+1; k++)
                    {

                        if (j != k)
                        {
                            expr.AddTerm(1, X[i, j, k]);
                        }

                    }

                }

                model.AddConstr(expr <= 1, $"R02_{j}");

            }

        }

        //limits the number of successors of the dummy jobs to a maximum of one on each machine
        public void Restricao03()
        {

            GRBLinExpr expr = new GRBLinExpr();

        

            for (int i = 0; i < qtdMaquinas; i++)
            {
                expr.Clear();
                for (int k = 1; k < qtdTarefas + 1 ; k++)
                {

                    expr.AddTerm(1, X[i, 0, k]);

                }

                model.AddConstr(expr <= 1, $"R03_{i}");

            }

        }

        //we ensure that jobs are properly linked in machine:
        //if a given job j is processed on a given machine i, a predecessor h must exist on the same machine.
        public void Restricao04()
        {

            GRBLinExpr expr = new GRBLinExpr();

            for (int i = 0; i < qtdMaquinas; i++)
            {

                for (int k = 1; k < qtdTarefas + 1; k++)
                {
                    for (int j = 1; j < qtdTarefas + 1; j++)
                    {
                        expr.Clear();
                        if (j != k) { 

                            
                            for(int h =0; h < qtdTarefas + 1; h++)
                            {

                                if (h != k)
                                {

                                    if (h != j)
                                    {

                                        expr.AddTerm(1, X[i, h, j]);

                                    }
                                
                                }

                            }

                            model.AddConstr(expr >= X[i, j, k], $"R04_{i}_{j}_{k}");

                        }


                    }

                }

            }

        }

        //control the completion times of the jobs at the machines
        public void Restricao05()
        {

            for (int i = 0; i < qtdMaquinas; i++)
            {

                for (int k = 1; k < qtdTarefas + 1; k++)
                {
                    for (int j = 0; j < qtdTarefas + 1; j++)
                    {

                        if (j != k)
                        {

                            //analisar se os setups e processamentos estarão pegando valor correto por que tem um zero na frente do j
                            model.AddConstr(C[i, k] + 999999 * (1 - X[i, j, k]) >= C[i, j] + setupTarefasPorMaquinas[i][j, k] + tarefasPorMaquinas[i][k], $"R05_{i}_{j}_{k}");

                        }

                    }

                }

            }

        }

        public void Restricao06()
        {

            for (int i = 0; i < qtdMaquinas; i++)
            {
                
                model.AddConstr(C[i, 0] == 0, $"R06_{i}");
            
            }

        }

        public void Restricao07()
        {

            for (int i = 0; i < qtdMaquinas; i++)
            {
                for (int k = 1; k < qtdTarefas + 1; k++) {

                    model.AddConstr(C[i, k] >= 0, $"R07_{i}_{k}");

                }

            }

        }

        public void Restricao08()
        {

            for (int i = 0; i < qtdMaquinas; i++)
            {

                for (int j = 1; j < qtdTarefas + 1; j++)
                {

                    model.AddConstr(Cmax[0] >= C[i, j], $"R08_{i}_{j}");

                }

            }

        }

    }
}

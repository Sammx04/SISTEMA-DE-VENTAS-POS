using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public class DBHelper
{
    private string cad_cn = "";

    public DBHelper(IConfiguration _config)
    {
        cad_cn = _config.GetConnectionString("cn1")!;
    }

    // 🔥 ESTE MÉTODO ES EL QUE TE FALTABA
    public SqlConnection ObtenerConexion()
    {
        return new SqlConnection(cad_cn);
    }

    void LlenarParametros(SqlCommand cmd, params object[] prms)
    {
        SqlCommandBuilder.DeriveParameters(cmd);

        int indice = 0;

        foreach (SqlParameter item in cmd.Parameters)
        {
            if (item.ParameterName != "@RETURN_VALUE")
            {
                if (prms[indice] == null)
                    item.Value = DBNull.Value;
                else
                    item.Value = prms[indice];

                indice++;
            }
        }
    }

    public void EjecutarSP(string NombreSP, params object[] Parametros)
    {
        using (SqlConnection cnx = new SqlConnection(cad_cn))
        {
            cnx.Open();

            SqlCommand comando = new SqlCommand(NombreSP, cnx);
            comando.CommandType = CommandType.StoredProcedure;

            if (Parametros.Length > 0)
                LlenarParametros(comando, Parametros);

            comando.ExecuteNonQuery();
        }
    }

    public SqlDataReader EjecutarSPDataReader(string NombreSP, params object[] Parametros)
    {
        SqlConnection cnx = new SqlConnection(cad_cn);
        cnx.Open();

        SqlCommand comando = new SqlCommand(NombreSP, cnx);
        comando.CommandType = CommandType.StoredProcedure;

        if (Parametros.Length > 0)
            LlenarParametros(comando, Parametros);

        return comando.ExecuteReader(CommandBehavior.CloseConnection);
    }

    public object EjecutarSPRetornaObject(string NombreSP, params object[] Parametros)
    {
        using (SqlConnection cnx = new SqlConnection(cad_cn))
        {
            cnx.Open();

            SqlCommand comando = new SqlCommand(NombreSP, cnx);
            comando.CommandType = CommandType.StoredProcedure;

            if (Parametros.Length > 0)
                LlenarParametros(comando, Parametros);

            return comando.ExecuteScalar();
        }
    }

    public DataTable EjecutarSPDataTable(string NombreSP, params object[] Parametros)
    {
        SqlDataAdapter adap = new SqlDataAdapter(NombreSP, cad_cn);
        adap.SelectCommand.CommandType = CommandType.StoredProcedure;

        if (Parametros.Length > 0)
            LlenarParametros(adap.SelectCommand, Parametros);

        DataTable tabla = new DataTable();
        adap.Fill(tabla);

        return tabla;
    }
}
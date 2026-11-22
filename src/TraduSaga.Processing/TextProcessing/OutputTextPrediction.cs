using Microsoft.ML.Data;

namespace TraduSaga.Processing.TextProcessing
{
    public class OutputTextPrediction
    {
        [ColumnName("Score")]
        public float Validity;
    }
}


// Struktura neuronu (entita)
public struct Neuron
{
    // Vystup neuronu
    public float output;

    // Chyba neuronu
    public float sigma;

    // Id neuronu vramci matice vah
    public int IndexW;

    public NeuronType type;

    public int num_of_inputs;
}
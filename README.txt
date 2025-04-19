## 🔧 Setting Up the Python Training Environment

This project uses [Unity ML-Agents 0.28.0](https://github.com/Unity-Technologies/ml-agents) and a GPU-enabled PyTorch backend for fast training.

### ✅ 1. Create and activate a virtual environment

```bash
python -m venv mlagents-env
# On Windows:
mlagents-env\Scripts\activate
# On macOS/Linux:
source mlagents-env/bin/activate


pip install torch==1.8.1+cu111 -f https://download.pytorch.org/whl/torch_stable.html
pip install -r requirements.txt
mlagents-learn config/fast.yaml --run-id=BackgammonRun --env=Builds/Backgammon.exe --time-scale=50 --num-envs=4


Start-Process -FilePath ".\Greedy_vs_Random.exe" -ArgumentList "-batchmode", "-nographics"

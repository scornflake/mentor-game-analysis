# Using Local LLMs with Mentor

Mentor supports OpenAI-compatible local LLM providers like LM Studio, Ollama, and LocalAI. However, there are significant caveats and limitations you should understand before attempting to use local models.

## TL;DR: Is It Worth It?

**Short answer:** Probably not for most users.

**Long answer:** Local LLMs can technically work with Mentor, but the experience is significantly degraded compared to cloud providers like Perplexity, Claude, or OpenAI. Unless you have specific privacy requirements or are experimenting with local AI, stick with cloud providers.

## Major Caveats

### 1. Hardware Requirements

Local LLMs are resource-intensive:

- **Memory (RAM):** Expect to use 8-32GB+ depending on model size
  - 7B models: ~8GB minimum
  - 13B models: ~16GB minimum
  - 24B models: 32GB (will use it all)
  - 34B+ models: 32GB+ recommended
  
- **CPU Usage:** Will max out CPU during inference
  - Inference time: 30 seconds to several minutes per analysis
  - Your computer will be sluggish during analysis
  - Good luck playing a game at the same time. Fat chance.
  
- **GPU (Optional but Recommended):** Dramatically speeds up inference
  - NVIDIA GPU with 8GB+ VRAM recommended
  - Without GPU, expect very long wait times
  - Still won't be pleasant

**Real-world test:** RTX 3090 (24GB VRAM) + 32GB RAM with a 24B parameter model (mistralai/magistral-small-2509) maxed out all available resources (OK I did also have IDEs and stuff open as well). This is basically a high-end gaming/workstation PC, and it still struggled.

### 2. Model Requirements

Not all models will work with Mentor. You need models that support:

✅ **Vision/Image Analysis** - Model must be able to process images
✅ **Tool Use/Function Calling** - Required for web search integration
✅ **Large Context Windows** - Need 4K+ tokens to fit images and prompts

**What I've tried:**
- mistralai/magistral-small-2509
- 

**Models that won't work:**
- Most text-only models (Llama 3.1, Mistral 7B, etc.)
- Models without function calling support
- Models with small context windows (<4K tokens)

### 3. LM Studio Configuration

If using LM Studio, you'll need to adjust settings:

**Increase Context Size:**
```
Settings → Context Length → Set to at least 8192 tokens
Something like 50,000+ token size (really needed for RAG)
```

Image analysis requires significant context:
- Base64 encoded images are large
- Prompts and system messages add overhead
- Structured output parsing needs room
- RAG/search results need even more space

**Recommended LM Studio Settings:**
- Context Length: 18192+ tokens
- Temperature: 0.3-0.7 (lower for more consistent structured output)
- Top P: 0.9

### 4. Performance: It's Slow

**Basic Analysis (No RAG):**
- Cloud providers: 5-15 seconds
- Local LLM (GPU): 30-90 seconds
- Local LLM (CPU only): 2-5 minutes

**With RAG/Web Search: It's REALLY Slow**

When you enable web search (Tavily):
1. Fetch search results: 2-5 seconds
2. Process and summarize articles: (currently done via Tavily, fast)
3. Final analysis: Local LLM needed (5+ minutes)

**Total time with RAG:**
- Local LLM (GPU): 2-5 minutes
- Local LLM (CPU only): "Tea" type time. 5-15 minutes or more

Your computer will be essentially unusable during this time.

### 5. Results: Dubious

Even when local models complete successfully:

❌ **Inconsistent Structured Output** - Local models struggle with JSON formatting
- May not follow the exact schema
- Parsing errors are common
- Retries may be needed

❌ **Lower Quality Recommendations** - Compared to GPT-4, Perplexity, or Claude:
- Less specific advice
- Generic recommendations
- May miss game-specific nuances
- Hallucinations more common

❌ **Context Limitations** - Even with large context:
- RAG results may not fit
- Might just explode with an exception

## When Local LLMs Make Sense

Consider local LLMs if:

✅ You have strict privacy requirements
✅ You have powerful hardware (32GB+ RAM, modern GPU, DGX Sitting around)
✅ You're experimenting with AI/ML
✅ You're okay with slow, lower-quality results
✅ You don't need web search/RAG features
✅ You can't afford the 5c per analysis that Claude, Perplexity charge

## Recommendations

**Things I Tried:**
1. **Perplexity** - Excellent for game analysis, built-in web search
2. **Claude (Anthropic)** - Great at following instructions, good vision


## Summary

Local LLMs with Mentor are technically possible but come with significant tradeoffs:

| Aspect | Cloud Providers | Local LLMs |
|--------|----------------|------------|
| Speed | ⚡ Fast (5-15s) | 🐌 Slow (30s-5min+) |
| Quality | ✅ High | ⚠️ Dubious |
| Setup | ✅ Easy | 🔧 Complex |
| Cost | 💰 Per-request | 🆓 Free (hardware costs) |
| Hardware | ☁️ Any | 💻 High-end required |
| RAG/Search | ✅ Fast | 🐌 REALLY slow |

**Bottom line:** If you care about usable results and don't want to wait minutes per analysis, use cloud providers. Local LLMs are best left for experimentation or specific privacy use cases.


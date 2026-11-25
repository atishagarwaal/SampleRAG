# Vector Profile (`vector-profile`) Explained

## What is `vector-profile`?

A **vector profile** is a setting in Azure AI Search that tells it how to store and search vectors (`contentVector`).

In `Services/SearchIndexService.cs`, it connects three things:

```text
contentVector (field) ──► vector-profile (profile) ──► hnsw-config (algorithm)
```

- **`contentVector`** — Stores the 1536 numbers (embedding) for each PDF chunk.
- **`vector-profile`** — Links `contentVector` to the search algorithm.
- **`hnsw-config`** — The fast search algorithm (HNSW) used to find similar vectors.

![Vector Profiles](images/06-vector-profiles.png)

---

## Why is it needed?

When you ask a question, the app needs to find the PDF chunks that are most similar in meaning.

- Checking every chunk one by one gets slow as you add more documents.
- `vector-profile` tells Azure AI Search to organize the vectors so it can find the best matches quickly.

---

## How HNSW Works (Simply)

Think of **HNSW** like a map with zoom levels:

1. **Top level (zoomed out):** Quickly jumps to the right general area of similar topics.
2. **Lower levels (zoomed in):** Narrows down to the closest matching chunks.
3. **Result:** Finds the best matches fast without checking every single chunk.

---

## How It Works in This App

1. **Startup:** PDF text → `text-embedding-3-small` → 1536 numbers → saved in `contentVector` using `vector-profile`.
2. **Question:** Your question → `text-embedding-3-small` → 1536 numbers.
3. **Search:** Azure AI Search uses `vector-profile` (`hnsw-config`) to return the **top 5** closest chunks.
4. **Answer:** Those 5 chunks are sent to `gpt-4.1-nano` to generate your answer.

> **Note:** The size `1536` must match the embedding model (`text-embedding-3-small` outputs 1536 numbers).

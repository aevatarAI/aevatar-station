# 📊 **Metrics Accuracy Formulas Reference**

**Document Version**: 1.0  
**Created**: June 9, 2025  
**Purpose**: Statistical formulas for determining sampling rates and aggregation windows to achieve target accuracy levels

---

## 🧮 **Core Statistical Formulas**

### **1. Sample Size per Window**
```
Samples per Window = Traffic Rate × Sampling Rate × Window Size

Where:
- Traffic Rate: requests/events per unit time
- Sampling Rate: percentage of events captured (0.01 = 1%)
- Window Size: aggregation time period
```

### **2. Configuration Relationship**
```
Sampling Rate × Window Size = Required Samples / Traffic Rate

This is the fundamental relationship for determining configurations
```

### **3. Required Samples for Target Accuracy**
```
Required Samples = f(Metric Type, Accuracy Target, Confidence Level, Data Distribution)
```

---

## 📈 **Accuracy Formulas by Metric Type**

### **A. Averages/Means (Normal Distribution)**

#### **Margin of Error Formula**
```
Margin of Error = z × (σ / √n)

Where:
- z = confidence factor (1.96 for 95%, 2.58 for 99%)
- σ = standard deviation
- n = sample size
```

#### **Required Sample Size**
```
n = (z × σ / E)²

Where:
- E = acceptable margin of error
- For relative error: E = target_accuracy_percent × μ / 100
- For absolute error: E = absolute_error_value
```

#### **Coefficient of Variation Method**
```
n = (z × CV / relative_error)²

Where:
- CV = Coefficient of Variation = σ/μ
- relative_error = target_accuracy_percent / 100
```

### **B. Percentiles (Non-parametric)**

#### **Empirical Sample Requirements**
```
P50 (Median): n ≥ 20 × (20/target_accuracy_percent)²
P90: n ≥ 50 × (20/target_accuracy_percent)²
P95: n ≥ 100 × (20/target_accuracy_percent)²
P99: n ≥ 500 × (20/target_accuracy_percent)²

Base formula: n ≥ base_samples × (20/target_accuracy_percent)²
```

#### **Order Statistics Confidence Intervals**
```
For percentile p with n samples:
Confidence interval rank: k ± z√(np(1-p))

Where k = np (the expected rank of the percentile)
```

### **C. Error Rates (Binomial Distribution)**

#### **Proportion Confidence Interval**
```
Margin of Error = z × √(p(1-p)/n)

Required sample size:
n = (z/E)² × p(1-p)

Where:
- p = true error rate
- E = acceptable margin of error (absolute)
```

#### **Conservative Estimate (Unknown p)**
```
n = (z/E)² × 0.25

Uses p=0.5 for maximum variance
```

---

## 🎯 **Practical Calculation Examples**

### **Example 1: Average Latency (80% Accuracy)**
```
Given:
- True mean (μ) = 100ms
- Standard deviation (σ) = 50ms
- Target accuracy = ±20% (±20ms)
- Confidence level = 95%

Calculation:
CV = σ/μ = 50/100 = 0.5
z = 1.96
relative_error = 0.20

n = (1.96 × 0.5 / 0.20)² = (4.9)² ≈ 24 samples

Result: Need 24 samples per window for 80% accuracy
```

### **Example 2: P95 Latency (80% Accuracy)**
```
Given:
- Target: P95 with ±20% accuracy
- Base requirement for P95: 100 samples

Calculation:
n = 100 × (20/20)² = 100 × 1 = 100 samples

Result: Need 100 samples per window for 80% accuracy
```

### **Example 3: Error Rate (90% Accuracy)**
```
Given:
- True error rate (p) = 0.01 (1%)
- Target accuracy = ±0.1% absolute error
- Confidence level = 95%

Calculation:
z = 1.96
E = 0.001
n = (1.96/0.001)² × 0.01 × 0.99 = 3,841,600 × 0.0099 ≈ 38,032

Result: Need ~38,000 samples for accurate error rate measurement
```

---

## 📊 **Configuration Lookup Tables**

### **Average Latency (±20% accuracy, 95% confidence)**
| Traffic (req/min) | CV=0.3 | CV=0.5 | CV=1.0 | Required Samples |
|-------------------|--------|--------|--------|------------------|
| **10,000** | 0.09% | 0.24% | 0.96% | 9/24/96 |
| **1,000** | 0.9% | 2.4% | 9.6% | 9/24/96 |
| **100** | 9% | 24% | 96% | 9/24/96 |

### **P95 Latency (±20% accuracy)**
| Traffic (req/min) | 1min Window | 5min Window | 10min Window | Required Samples |
|-------------------|-------------|-------------|--------------|------------------|
| **10,000** | 1% | 0.2% | 0.1% | 100 |
| **1,000** | 10% | 2% | 1% | 100 |
| **100** | 100% | 20% | 10% | 100 |

### **Error Rate (±0.1% absolute accuracy, 95% confidence)**
| Traffic (req/min) | True Rate 0.1% | True Rate 1% | True Rate 5% |
|-------------------|----------------|--------------|--------------|
| **10,000** | 38% | 38% | 8% |
| **1,000** | **Impossible** | **Impossible** | 80% |
| **100** | **Impossible** | **Impossible** | **Impossible** |

---

## 🧮 **Python Calculator Functions**

### **Average/Mean Accuracy Calculator**
```python
import math

def required_samples_mean(target_accuracy_percent, confidence_level=0.95, cv=0.5):
    """
    Calculate required samples for mean accuracy
    
    Args:
        target_accuracy_percent: Target accuracy (e.g., 20 for ±20%)
        confidence_level: Statistical confidence (0.95 or 0.99)
        cv: Coefficient of variation (σ/μ)
    
    Returns:
        Required sample size
    """
    z_scores = {0.95: 1.96, 0.99: 2.58}
    z = z_scores[confidence_level]
    relative_error = target_accuracy_percent / 100
    
    return math.ceil((z * cv / relative_error) ** 2)

# Example usage:
# required_samples_mean(20, 0.95, 0.5) → 24 samples
```

### **Percentile Accuracy Calculator**
```python
def required_samples_percentile(percentile, target_accuracy_percent):
    """
    Calculate required samples for percentile accuracy
    
    Args:
        percentile: Target percentile (50, 90, 95, 99)
        target_accuracy_percent: Target accuracy (e.g., 20 for ±20%)
    
    Returns:
        Required sample size
    """
    base_samples = {50: 20, 90: 50, 95: 100, 99: 500}
    
    if percentile not in base_samples:
        raise ValueError("Percentile must be 50, 90, 95, or 99")
    
    accuracy_factor = (20 / target_accuracy_percent) ** 2
    return math.ceil(base_samples[percentile] * accuracy_factor)

# Example usage:
# required_samples_percentile(95, 20) → 100 samples
```

### **Configuration Calculator**
```python
def calculate_sampling_configs(traffic_per_minute, required_samples, max_window_minutes=10):
    """
    Calculate possible sampling rate and window size combinations
    
    Args:
        traffic_per_minute: Request rate per minute
        required_samples: Required samples per window
        max_window_minutes: Maximum acceptable window size
    
    Returns:
        List of viable configurations
    """
    configs = []
    window_options = [0.5, 1, 2, 5, 10, 15, 30]
    
    for window_minutes in window_options:
        if window_minutes > max_window_minutes:
            continue
        
        sampling_rate = required_samples / (traffic_per_minute * window_minutes)
        
        if 0.001 <= sampling_rate <= 1.0:  # 0.1% to 100%
            configs.append({
                'sampling_rate_percent': round(sampling_rate * 100, 2),
                'window_minutes': window_minutes,
                'samples_per_window': required_samples,
                'storage_points_per_hour': 60 / window_minutes
            })
    
    return configs

# Example usage:
# calculate_sampling_configs(1000, 100, 5)
# → [{'sampling_rate_percent': 20.0, 'window_minutes': 0.5, ...}, ...]
```

---

## 📋 **Quick Reference Formulas**

### **Common Accuracy Targets**
```
90% Accuracy (±10% error):
- Means: n = (1.96 × CV / 0.10)²
- P95: n = 100 × (20/10)² = 400 samples

80% Accuracy (±20% error):
- Means: n = (1.96 × CV / 0.20)²
- P95: n = 100 × (20/20)² = 100 samples

70% Accuracy (±30% error):
- Means: n = (1.96 × CV / 0.30)²
- P95: n = 100 × (20/30)² = 44 samples
```

### **Traffic-Based Quick Estimates**
```
High Traffic (>1000 req/min):
- Can afford low sampling rates (1-10%)
- Use smaller windows (1-5min)

Medium Traffic (100-1000 req/min):
- Need moderate sampling (10-50%)
- Use medium windows (1-10min)

Low Traffic (<100 req/min):
- Need high sampling (50-100%)
- Use larger windows (5-30min)
```

---

## ⚠️ **Important Assumptions & Limitations**

### **Statistical Assumptions**
1. **Normal Distribution**: Mean formulas assume normal or near-normal distribution
2. **Independent Samples**: Sampling must be random and independent
3. **Stationary Process**: Traffic patterns should be relatively stable during measurement
4. **Representative Sampling**: Samples must represent the full population

### **Practical Limitations**
1. **Minimum Sample Sizes**: Very small sample sizes (<10) are unreliable regardless of formulas
2. **Extreme Percentiles**: P99+ require very large sample sizes, may be impractical
3. **Bursty Traffic**: Formulas assume steady traffic; bursts can affect accuracy
4. **System Overhead**: High sampling rates can impact system performance

### **Formula Accuracy**
1. **Empirical Basis**: Percentile formulas are based on empirical studies, not pure theory
2. **Distribution Dependent**: Actual accuracy depends on underlying data distribution
3. **Confidence Intervals**: Formulas provide expected accuracy, not guarantees

---

## 🔗 **References & Further Reading**

### **Statistical References**
- Central Limit Theorem applications in monitoring
- Order statistics for percentile estimation
- Binomial confidence intervals for error rates

### **Industry Standards**
- SRE practices for monitoring accuracy
- Prometheus recording rule best practices
- OpenTelemetry sampling strategies

### **Tools & Implementation**
- Prometheus histogram bucket configuration
- Grafana query optimization
- OTEL SDK sampling configuration

---

**Document Maintenance**: Update formulas based on empirical validation and industry best practices. 
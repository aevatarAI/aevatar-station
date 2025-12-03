# Event Forwarding E2E Test

This sample demonstrates and tests the advanced **hierarchical event forwarding system** in the AevatarStation framework. It validates that events and configurations are correctly forwarded between parent and child agents based on their `EventDirection` property, with support for complex multi-level agent hierarchies.

## Overview

The EventForwardingE2E test creates a sophisticated 3-level hierarchical structure of agents and verifies that events are forwarded correctly based on their direction:

- **Up events**: Forward from children to parents through multiple levels
- **Down events**: Forward from parents to children through multiple levels  
- **UpThenDown events**: Go up to parent, then parent broadcasts down to siblings
- **Bidirectional events**: Forward in both directions simultaneously with loop prevention
- **Single Hop events**: Limited to one level in specified direction
- **Multi-Level events**: Traverse multiple levels with configurable hop limits

## Architecture

### Advanced Event Direction System

The framework implements a sophisticated **bidirectional stream-based architecture** that supports both **Events** (`TEvent`) and **Configurations** (`TConfiguration`) with optimized batch subscriptions and automatic subscription restoration.

**Stream Architecture:**
- **Direction 0**: Upward streams (child owns, parents subscribe)
- **Direction 1**: Downward streams (parent owns, children subscribe)

**Stream ID Format:** `{GrainId}.{Direction}.{EventType}`

```csharp
// Framework provides unified publishing method
await this.PublishEventByDirectionAsync(@event);

// Internal stream ID generation:
// Upward: "{childGrainId}.0.TestEvent"
// Downward: "{parentGrainId}.1.TestEvent"
```

**Supported Event Directions:**
- `EventDirection.Up`: Publish to child's upward stream (direction 0)
- `EventDirection.Down`: Publish to parent's downward stream (direction 1)  
- `EventDirection.UpThenDown`: First up to parent, then parent broadcasts down
- `EventDirection.Bidirectional`: Simultaneous up and down with loop prevention

### Enhanced Subscription Management

**Batch Subscription Optimization:**
The framework uses efficient batch subscription patterns for scalable hierarchical communication:

```csharp
// Optimized batch subscriptions during activation
await SubscribeToManyParentByIdAsync(State.Parents);     // Batch parent subscriptions
await SubscribeToManyChildByIdAsync(State.Children);     // Batch child subscriptions
```

**Automatic Subscription Restoration:**
- `ResumeForwardingSubscriptionsAsync()` automatically restores all parent and child subscriptions during grain activation
- Prevents subscription loss during deactivation/reactivation cycles
- Uses parallel batch operations for maximum performance

### Complex Hierarchical Test Structure

The demo creates a sophisticated **3-level family hierarchy** with proper sibling, uncle, and cousin relationships:

```
                             Root
                              │
                ┌─────────────┼─────────────┐
                │             │             │
              Uncle1       Parent        Uncle2
                │             │             │
         ┌──────┴──────┐ ┌────┴────┐ ┌──────┴──────┐
         │             │ │         │ │             │
    Uncle1Child1  Uncle1Child2  Child1  Child2  Uncle2Child1  Uncle2Child2
```

**Agent Relationships:**
- **Level 1**: Root (top-level coordinator)
- **Level 2**: Uncle1, Parent, Uncle2 (siblings)
- **Level 3**: Each Level 2 agent has distinct children:
  - Uncle1: Uncle1Child1, Uncle1Child2
  - Parent: Child1, Child2  
  - Uncle2: Uncle2Child1, Uncle2Child2

**Setup Process:**
1. **Initialize all 10 agents** with role-based names and hierarchy levels
2. **Register hierarchical relationships** using `RegisterAsync` - automatically establishes bidirectional communication
3. **Verify complex forwarding patterns** including sibling, uncle, and cousin communication

**Key Registration Methods:**
- `RegisterAsync()` - Establishes complete bidirectional communication:
  - Child: `SubscribeToParentAsync()` (subscribes to parent's downward streams)
  - Parent: `SubscribeToChildAsync()` (subscribes to child's upward streams)
  - Supports both `TEvent` and `TConfiguration` streams
- `RegisterManyAsync()` - Batch registration for multiple children using optimized batch subscriptions

### Unified Event Model

The demo uses a **single `TestEvent` class** with configurable properties to test all forwarding scenarios:

```csharp
public class TestEvent : EventBase
{
    public string TestMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public string TestId { get; set; }
    public string OriginAgentId { get; set; }
    public List<string> ForwardingPath { get; set; }
    public int CurrentHopCount { get; set; }  // Tracks hops taken
}
```

**Event Direction Behaviors:**
1. **Up Events**: `Direction.Up` - Children → Parents (Child1 → Parent → Root)
2. **Down Events**: `Direction.Down` - Parents → Children (Root → All descendants)
3. **UpThenDown Events**: `Direction.UpThenDown` - Up to parent, then parent broadcasts down to siblings
4. **Bidirectional Events**: `Direction.Bidirectional` - Simultaneous up and down with loop prevention
5. **Single Hop Events**: Limited to 1 hop with `MaxHopCount=1`
6. **Multi-Level Events**: Configurable hop limits or unlimited (`MaxHopCount=-1`)

**Hop Counting System:**
- **`MaxHopCount`**: Immutable limit set when creating event (-1 = unlimited)
- **`CurrentHopCount`**: Incremented at each forwarding step
- **Loop Prevention**: Uses `Publishers` list to prevent circular forwarding

## Prerequisites

1. **MongoDB**: Running on `localhost:27017`
2. **Kafka**: Running on `localhost:9092`
3. **Orleans Silo**: The AevatarStation silo must be running

## Running the Test

```bash
cd /Users/charles/workspace/github/aevatar-station/station/samples/EventForwardingE2E
dotnet run
```

## Interactive Menu System

The demo features a comprehensive interactive menu system for testing different forwarding scenarios:

```
=====================================
🧪 EventForwardingE2E Test Menu
=====================================

Available Tests:
  1. 🎯 Run All Tests
  2. 🔼 Upward Events Test
  3. 🔽 Downward Events Test
  4. 🔄⬇️ UpThenDown Events Test
  5. 🔄 Bidirectional Events Test
  6. 1️⃣⬆️ Single Hop Upward Test
  7. 1️⃣⬇️ Single Hop Downward Test
  8. 🎯 Multi-Level Events Test
  9. 🌳 Visualize Node Relationships
  10. 🚪 Exit
```

### Menu Options

- **Option 1**: **Run All Tests** - Executes all test categories in sequence with timing information
- **Options 2-5**: **Core Event Direction Tests** - Test fundamental forwarding behaviors
- **Options 6-7**: **Single Hop Tests** - Granular testing of hop limiting in each direction
- **Option 8**: **Multi-Level Test** - Complex hop counting and multi-level traversal
- **Option 9**: **Relationship Visualization** - Display the hierarchical structure with ASCII art
- **Option 10**: **Exit** - Quit the demo application

### Enhanced Navigation Features

- **Flexible Input**: Enter numbers (1-10) or aliases ('q', 'quit', 'exit')
- **Continuous Testing**: Return to menu after each test for repeated execution
- **Clear Interface**: Console clears between operations for better readability
- **Comprehensive Feedback**: Detailed test results with timing and verification status

## Comprehensive Test Scenarios

### Hierarchical Structure Overview
```
Level 1:           Root (coordinator)
                    │
Level 2:    Uncle1 ├─ Parent ├─ Uncle2 (siblings)
             │          │         │
Level 3:  U1C1,U1C2  C1,C2  U2C1,U2C2 (cousins)
```

### 1. **Upward Events Test** 🔼
- **Publisher**: Child1 → Parent → Root
- **Expected**: 2 recipients (Parent, Root)
- **Verification**: Proper upward propagation through hierarchy levels

### 2. **Downward Events Test** 🔽  
- **Publisher**: Root → All Level 2 & 3 agents
- **Expected**: 9 recipients (3 Level 2 + 6 Level 3 agents)
- **Verification**: Comprehensive downward broadcast to all descendants

### 3. **UpThenDown Events Test** 🔄⬇️ *(Advanced Behavior)*
- **Publisher**: Child1 → Parent → Siblings & Cousins
- **Process**: Up to parent, then parent broadcasts down to extended family
- **Expected**: Uncle1Child1, Child2, Uncle2Child1 receive events
- **Verification**: Complex cross-family communication patterns

### 4. **Bidirectional Events Test** 🔄 *(Loop Prevention)*
- **Publisher**: Parent (central position)
- **Upward Flow**: Parent → Root
- **Downward Flow**: Parent → Child1, Child2 + cousin notifications
- **Features**: Publisher tracking prevents infinite loops
- **Verification**: Simultaneous bidirectional flow with safety mechanisms

### 5. **Single Hop Upward Test** 1️⃣⬆️
- **Publisher**: Child1 → Parent only (stops at Root)
- **MaxHopCount**: 1
- **Verification**: Strict hop limiting prevents over-propagation

### 6. **Single Hop Downward Test** 1️⃣⬇️
- **Publisher**: Root → Level 2 only (stops at Level 3)  
- **MaxHopCount**: 1
- **Verification**: Level 3 agents should not receive events

### 7. **Multi-Level Events Test** 🎯 *(Complex Traversal)*
- **Publishers**: Various agents with different hop limits
- **Scenarios**: Unlimited (-1), Limited (2), and Custom hop counts
- **Verification**: `CurrentHopCount` tracking and `MaxHopCount` enforcement

### 8. **Relationship Visualization** 🌳
- **ASCII Tree**: Visual representation of the 10-agent hierarchy
- **Family Relationships**: Clear parent-child, sibling, uncle, cousin mappings
- **Agent Details**: Grain IDs and connection patterns

## Event Tracking

Each test agent tracks received events with:

- **Event ID**: Unique identifier for correlation
- **Event Type**: Class name of the event
- **Direction**: The forwarding direction (Up/Down/UpAndDown)
- **Origin**: Which agent originally published the event
- **Forwarding Path**: Complete path the event traveled
- **Timestamp**: When the event was received

## Expected Output

```
=== EventForwarding E2E Test ===
🏗️ Initializing sophisticated hierarchical event forwarding system...

✅ Connected to Orleans cluster successfully

🧪 Setting up complex 10-agent family hierarchy...
 🆔 Root: 0x2c1f8b3a9d4e5f6c7a8b9c0d1e2f3a4b5c6d7e8f
 ➕ Root (0x2c1f8b3a9d4e5f6c7a8b9c0d1e2f3a4b5c6d7e8f) registered Uncle1 (0x5a7b9c1d3e5f7a9b1c3d5e7f9a1b3c5d7e9f1a3c)
 ➕ Root (0x2c1f8b3a9d4e5f6c7a8b9c0d1e2f3a4b5c6d7e8f) registered Parent (0x8d9f1a3c5e7b9d1f3a5c7e9b1d3f5a7c9e1b3d5f)
 ➕ Root (0x2c1f8b3a9d4e5f6c7a8b9c0d1e2f3a4b5c6d7e8f) registered Uncle2 (0x1b3d5f7a9c1e3f5a7c9e1b3d5f7a9c1e3f5a7c9e)
 ➕ Uncle1 (0x5a7b9c1d3e5f7a9b1c3d5e7f9a1b3c5d7e9f1a3c) registered Uncle1Child1 (0x9e1b3d5f7a9c1e3f5a7c9e1b3d5f7a9c1e3f5a7c)
 ➕ Uncle1 (0x5a7b9c1d3e5f7a9b1c3d5e7f9a1b3c5d7e9f1a3c) registered Uncle1Child2 (0x2f4a6c8e0b2d4f6a8c0e2b4d6f8a0c2e4b6d8f0a)
 ➕ Parent (0x8d9f1a3c5e7b9d1f3a5c7e9b1d3f5a7c9e1b3d5f) registered Child1 (0x7c9e1b3d5f7a9c1e3f5a7c9e1b3d5f7a9c1e3f5a)
 ➕ Parent (0x8d9f1a3c5e7b9d1f3a5c7e9b1d3f5a7c9e1b3d5f) registered Child2 (0x4f6a8c0e2b4d6f8a0c2e4b6d8f0a2c4e6b8d0f2a)
 ➕ Uncle2 (0x1b3d5f7a9c1e3f5a7c9e1b3d5f7a9c1e3f5a7c9e) registered Uncle2Child1 (0xa3c5e7b9d1f3a5c7e9b1d3f5a7c9e1b3d5f7a9c1)
 ➕ Uncle2 (0x1b3d5f7a9c1e3f5a7c9e1b3d5f7a9c1e3f5a7c9e) registered Uncle2Child2 (0x6d8f0a2c4e6b8d0f2a4c6e8d0f2a4c6e8d0f2a4c)

✅ Complex hierarchy established successfully! (10 agents, 9 relationships)

=====================================
🧪 EventForwardingE2E Test Menu
=====================================

Available Tests:
  1. 🎯 Run All Tests
  2. 🔼 Upward Events Test
  3. 🔽 Downward Events Test
  4. 🔄⬇️ UpThenDown Events Test
  5. 🔄 Bidirectional Events Test
  6. 1️⃣⬆️ Single Hop Upward Test
  7. 1️⃣⬇️ Single Hop Downward Test
  8. 🎯 Multi-Level Events Test
  9. 🌳 Visualize Node Relationships
  10. 🚪 Exit

Choose your option (1-10): 9

🌳 Node Relationship Visualization
=====================================

Family Hierarchy Structure: 
                                       Root
                                        │
                ┌───────────----------──┼─────────────----------------┐
                │                       │                             │
              Uncle1                  Parent                        Uncle2
                │                       │                             │
         ┌──────┴──────┐           ┌────┴────┐                ┌─────-─┴──────┐
         │             │           │         │                │              │
    Uncle1Child1  Uncle1Child2    Child1  Child2          Uncle2Child1  Uncle2Child2

Family Relationships Summary:
• Siblings: Uncle1, Parent, Uncle2 (all children of Root)
• Uncle1's Children: Uncle1Child1, Uncle1Child2
• Parent's Children: Child1, Child2  
• Uncle2's Children: Uncle2Child1, Uncle2Child2
• Cousins: All Level 3 agents are cousins to each other

Event Flow Patterns:
• Upward: Children → Parents → Root
• Downward: Root → Level 2 → Level 3
• UpThenDown: Child → Parent → Siblings & Cousins
• Bidirectional: Simultaneous Up + Down with loop prevention

Agent Details:
┌─────────────────┬──────────────────────────────────────────────────────┐
│ Agent Name      │ Grain ID (Truncated)                                 │
├─────────────────┼──────────────────────────────────────────────────────┤
│ Root            │ 0x2c1f8b3a9d4e5f6c...                                │
│ Uncle1          │ 0x5a7b9c1d3e5f7a9b...                                │
│ Parent          │ 0x8d9f1a3c5e7b9d1f...                                │
│ Uncle2          │ 0x1b3d5f7a9c1e3f5a...                                │
│ Uncle1Child1    │ 0x9e1b3d5f7a9c1e3f...                                │
│ Uncle1Child2    │ 0x2f4a6c8e0b2d4f6a...                                │
│ Child1          │ 0x7c9e1b3d5f7a9c1e...                                │
│ Child2          │ 0x4f6a8c0e2b4d6f8a...                                │
│ Uncle2Child1    │ 0xa3c5e7b9d1f3a5c7...                                │
│ Uncle2Child2    │ 0x6d8f0a2c4e6b8d0f...                                │
└─────────────────┴──────────────────────────────────────────────────────┘

Press any key to continue...

Choose your option (1-10): 1

🎯 Running All Tests (Comprehensive Suite)
=========================================

🔼 Testing Upward Events (Child1 → Parent → Root)
Expected: Child1 publishes → Parent receives → Root receives
Publisher: Child1 (TestEvent: "Upward event from Child1")
  Child1 received: 0 events ✅
  Parent received: 1 events ✅
  Root received: 1 events ✅
  Uncle1, Uncle2, all cousins: 0 events ✅
✅ Upward event forwarding working correctly
  📍 Forwarding path: Child1(Level3) → Parent(Level2) → Root(Level1)

🔽 Testing Downward Events (Root → All descendants)
Expected: Root publishes → All 9 descendants receive
Publisher: Root (TestEvent: "Downward broadcast from Root")
  Root received: 0 events ✅
  Level 2 agents (Uncle1, Parent, Uncle2): 3 events ✅
  Level 3 agents (all 6 children): 6 events ✅
  Total expected recipients: 9, actual: 9 ✅
✅ Downward event forwarding working correctly
  📍 Broadcast reached complete family tree

🔄⬇️ Testing UpThenDown Events (Child1 → Parent → Extended family)
Expected: Child1 → Parent → Child2 + Uncle cousins
Publisher: Child1 (TestEvent: "UpThenDown from Child1 to family")
  Child1 received: 0 events ✅
  Parent received: 1 events ✅  
  Child2 received: 1 events ✅ (sibling notification)
  Uncle1Child1, Uncle2Child1: 2 events ✅ (cousin notifications)
  Uncle1Child2, Uncle2Child2: 0 events ✅ (not first cousins)
✅ UpThenDown event forwarding working correctly
  📍 Cross-family communication established

🔄 Testing Bidirectional Events (Parent → Up + Down simultaneously)
Expected: Parent → Root (up) + Children & Cousins (down)
Publisher: Parent (TestEvent: "Bidirectional from Parent")
  Upward flow: Root received 1 events ✅
  Downward flow: Child1, Child2 received 2 events ✅
  Publisher tracking: Parent not in recipients ✅ (loop prevention)
  Hop count respected: MaxHopCount=3, all within limit ✅
✅ Bidirectional event forwarding working correctly
  📍 Simultaneous multi-directional flow achieved

1️⃣⬆️ Testing Single Hop Upward (Child1 → Parent only)
Expected: MaxHopCount=1, stops at Parent
Publisher: Child1 (TestEvent: "Single hop upward", MaxHopCount=1)
  Child1 received: 0 events ✅
  Parent received: 1 events ✅ (hop 1)
  Root received: 0 events ✅ (would be hop 2, blocked)
✅ Single hop upward limiting working correctly

1️⃣⬇️ Testing Single Hop Downward (Root → Level 2 only)  
Expected: MaxHopCount=1, stops at Level 2
Publisher: Root (TestEvent: "Single hop downward", MaxHopCount=1)
  Root received: 0 events ✅
  Level 2 (Uncle1, Parent, Uncle2): 3 events ✅ (hop 1)
  Level 3 agents: 0 events ✅ (would be hop 2, blocked)
✅ Single hop downward limiting working correctly

🎯 Testing Multi-Level Events with Complex Hop Limits
Expected: Various hop scenarios with CurrentHopCount tracking
Test 1 - Unlimited hops (MaxHopCount=-1):
  Publisher: Uncle1Child1, reached all 9 other agents ✅
Test 2 - Limited hops (MaxHopCount=2):
  Publisher: Uncle2, reached 7 of 9 agents ✅
  Blocked: Uncle2Child1, Uncle2Child2 (would exceed hop limit)
Test 3 - Hop count verification:
  Sample event CurrentHopCount progression: 0→1→2 ✅
  MaxHopCount remained immutable: 2 ✅
✅ Multi-level event forwarding working correctly

🎉 All EventForwarding Tests Completed Successfully!
📊 Test Summary:
   ✅ Upward Events: PASSED
   ✅ Downward Events: PASSED  
   ✅ UpThenDown Events: PASSED
   ✅ Bidirectional Events: PASSED
   ✅ Single Hop Upward: PASSED
   ✅ Single Hop Downward: PASSED
   ✅ Multi-Level Events: PASSED
   
🏆 Total: 7/7 tests passed
⏱️ Total execution time: 3.2 seconds
🚀 Enterprise-scale hierarchical communication verified!
```

## Key Features Demonstrated

### 🎯 **Advanced Event Forwarding System**
1. **Unified Event Model**: Single `TestEvent` class handles all forwarding scenarios
2. **Dual Stream Support**: Both `TEvent` and `TConfiguration` forwarding
3. **Direction-Based Routing**: `Up`, `Down`, `UpThenDown`, `Bidirectional` behaviors
4. **Intelligent Hop Control**: Immutable `MaxHopCount` with tracking via `CurrentHopCount`
5. **Loop Prevention**: Publisher tracking prevents circular forwarding

### ⚡ **Performance Optimizations**
1. **Batch Subscriptions**: `SubscribeToManyParentAsync` and `SubscribeToManyChildAsync`
2. **Activation Recovery**: `ResumeForwardingSubscriptionsAsync` restores subscriptions automatically
3. **Parallel Operations**: Concurrent batch subscription processing
4. **Deep Copy Safety**: Prevents shared state mutations during stream broadcasting

### 🏗️ **Architectural Excellence**
1. **Hierarchical Scaling**: 10-agent complex family relationships
2. **Bidirectional Communication**: Automatic parent-child subscription setup
3. **Interactive Testing**: Comprehensive menu-driven test suite
4. **Relationship Visualization**: ASCII art hierarchy display

## Technical Architecture

### **Orleans Integration**
- **Kafka Streams**: Reliable event delivery with Orleans streaming
- **Grain Lifecycle**: Automatic subscription restoration during activation/deactivation
- **State Persistence**: MongoDB-backed agent state management
- **Distributed Computing**: Orleans cluster coordination

### **Event Processing**
- **Async Pipeline**: Fully asynchronous event handling with `Task.WhenAll` optimization
- **Exception Safety**: Comprehensive error handling and structured logging
- **Memory Management**: DeepCopier prevents shared state mutations
- **Stream Ownership**: Clear direction-based stream ownership patterns

### **Testing Infrastructure**
- **Verification Engine**: Event ID-based tracking for accurate test results
- **Timing Analysis**: Performance measurement for all test scenarios
- **Debug Support**: Detailed forwarding path tracking and grain ID logging

This comprehensive test suite validates the production-ready **hierarchical event forwarding framework**, demonstrating enterprise-scale capabilities for complex agent communication patterns.

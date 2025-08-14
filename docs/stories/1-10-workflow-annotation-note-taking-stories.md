---
Epic: 10. Workflow Annotation & Note-Taking System
---

# 1. Basic Note Creation and Placement

## User Story
As a user, I want to create sticky note-style annotations anywhere on the workflow canvas so that I can document my workflow design decisions and thoughts.

**Version:** v1.0

**Estimated Time:** 12 hours

### Acceptance Criteria
**Given** I am in the visual workflow designer  
**When** I right-click on an empty area of the canvas  
**Then** I see a context menu option to "Create Note"  

**Given** I have selected "Create Note" from the context menu  
**When** I click on it  
**Then** a new editable text note appears at the clicked location  

**Given** I have created a note on the canvas  
**When** I drag the note to a different position  
**Then** the note moves to the new location and maintains its position when I save the workflow

# 2. Rich Text Formatting for Notes

## User Story
As a user, I want to format text within my notes using bold, italic, bullet points, and links so that I can create well-structured documentation.

**Version:** v1.0

**Estimated Time:** 16 hours

### Acceptance Criteria
**Given** I have created a note on the canvas  
**When** I double-click on the note to edit it  
**Then** a rich text editor appears with formatting options (bold, italic, bullet points, links)  

**Given** I am editing a note with the rich text editor  
**When** I select text and apply formatting (bold, italic, etc.)  
**Then** the formatting is applied and visible in the note  

**Given** I have formatted text in a note  
**When** I save the workflow and reload it  
**Then** the text formatting is preserved and displayed correctly

# 3. Note Attachment to Workflow Nodes

## User Story
As a user, I want to attach notes to specific workflow nodes so that I can document node-specific configurations and behaviors.

**Version:** v1.0

**Estimated Time:** 14 hours

### Acceptance Criteria
**Given** I have workflow nodes on the canvas  
**When** I right-click on a specific node  
**Then** I see a context menu option to "Add Note"  

**Given** I have selected "Add Note" on a workflow node  
**When** I create the note  
**Then** the note is visually connected to the node with a line or indicator  

**Given** I have a note attached to a workflow node  
**When** I move the node to a different position  
**Then** the attached note moves with the node and maintains its relative position

# 4. Note Visual Styling and Types

## User Story
As a user, I want to use different note types with distinct visual styles and colors so that I can categorize my documentation (general notes, warnings, reminders).

**Version:** v1.0

**Estimated Time:** 10 hours

### Acceptance Criteria
**Given** I am creating a new note  
**When** I select the note type (General, Warning, Reminder, Documentation)  
**Then** the note displays with the appropriate color scheme and visual styling  

**Given** I have notes of different types on the canvas  
**When** I view the workflow  
**Then** I can clearly distinguish between note types based on their visual appearance  

**Given** I have created notes with different visual styles  
**When** I save and reload the workflow  
**Then** all note types maintain their correct visual styling and colors

# 5. Canvas Integration and Positioning

## User Story
As a user, I want notes to integrate seamlessly with canvas navigation (zoom, pan) so that they remain properly positioned and visible during workflow design.

**Version:** v1.0

**Estimated Time:** 14 hours

### Acceptance Criteria
**Given** I have notes positioned on the workflow canvas  
**When** I zoom in or out on the canvas  
**Then** the notes scale appropriately and maintain their relative positions  

**Given** I have notes on the canvas  
**When** I pan to different areas of the workflow  
**Then** the notes move with the canvas and remain visible in their correct positions  

**Given** I have notes and workflow nodes on the canvas  
**When** I use keyboard shortcuts for canvas navigation  
**Then** both notes and nodes respond consistently to the navigation commands 
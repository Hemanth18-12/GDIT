/**
 * Minimal synchronous publish/subscribe EventBus.
 */
export class EventBus {
  constructor() {
    this.listeners = new Map();
  }

  /**
   * Subscribe to an event.
   * @param {string} event - Name of the event.
   * @param {Function} callback - Callback function.
   * @returns {Function} Unsubscribe function.
   */
  on(event, callback) {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event).add(callback);
    return () => this.off(event, callback);
  }

  /**
   * Unsubscribe from an event.
   * @param {string} event - Name of the event.
   * @param {Function} callback - Callback function to remove.
   */
  off(event, callback) {
    if (this.listeners.has(event)) {
      const callbacks = this.listeners.get(event);
      callbacks.delete(callback);
      if (callbacks.size === 0) {
        this.listeners.delete(event);
      }
    }
  }

  /**
   * Publish an event with optional payload data.
   * @param {string} event - Name of the event.
   * @param {*} data - Payload data.
   */
  emit(event, data) {
    if (!this.listeners.has(event)) return;
    for (const callback of this.listeners.get(event)) {
      try {
        callback(data);
      } catch (err) {
        console.error(`[EventBus] Error handling event "${event}":`, err);
      }
    }
  }
}

export default EventBus;


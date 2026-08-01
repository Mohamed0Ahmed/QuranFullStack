
import 'zone.js/testing';

// jsdom gives every element a zero-size box, so the CDK's focusable check rejects every
// `[cdkFocusInitial]` target and warns once per modal open — 60 warnings across the abwab suite
// alone, each dumping the whole element. The check can never pass here, so the warning says
// nothing about the markup: drop exactly it, and let every other warning through.
const CDK_FOCUS_INITIAL_JSDOM_WARNING = `Element matching '[cdkFocusInitial]' is not focusable.`;
const passThroughWarn = console.warn.bind(console);
console.warn = (...args: unknown[]): void => {
  if (typeof args[0] === 'string' && args[0].includes(CDK_FOCUS_INITIAL_JSDOM_WARNING)) {
    return;
  }
  passThroughWarn(...args);
};

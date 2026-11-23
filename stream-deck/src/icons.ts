import type { Kind } from "./controls.js";

const drawings: Record<Kind, string> = {
  pointer: '<path d="M43 112V62a9 9 0 0 1 18 0v9-38a9 9 0 0 1 18 0v37l10-13a9 9 0 0 1 15 10l-16 34c-8 18-30 23-45 11Z"/>',
  size: '<rect x="50" y="50" width="44" height="44" rx="5"/><path d="M22 47V22h25M22 22l23 23M97 22h25v25M122 22 99 45M22 97v25h25M22 122l23-23M97 122h25V97M122 122l-23-23"/>',
  sound: '<path d="M24 58h22l27-23v74L46 86H24Z"/><path d="M89 52q20 20 0 40M103 38q34 34 0 68"/>',
  color: '<path d="M72 23a49 49 0 1 0 0 98h7c12 0 15-13 6-20-8-7-3-17 7-17h8c28 0 27-42-3-55a57 57 0 0 0-25-6Z"/><circle cx="45" cy="55" r="5"/><circle cx="68" cy="43" r="5"/><circle cx="91" cy="51" r="5"/><circle cx="39" cy="80" r="5"/>',
  text: '<path d="M29 37h86M72 37v73M53 110h38M29 37v16M115 37v16"/>',
  capture: '<rect x="23" y="40" width="98" height="71" rx="10"/><path d="m46 40 8-15h36l8 15"/><circle cx="72" cy="76" r="22"/>',
  clear: '<path d="M30 40h84M56 40V26h32v14M40 40l6 78h52l6-78M61 59v39M83 59v39"/>',
};

export function iconSvg(kind: Kind, background?: string): string {
  const backdrop = background ? `<rect x="4" y="4" width="136" height="136" rx="28" fill="${background}"/>` : "";
  return `<svg xmlns="http://www.w3.org/2000/svg" width="144" height="144" viewBox="0 0 144 144">${backdrop}<g fill="none" stroke="#eee7f5" stroke-width="7" stroke-linecap="round" stroke-linejoin="round">${drawings[kind]}</g></svg>`;
}

export function brandedIcon(kind: Kind, brand: string, selected = false, color?: string): string {
  const marker = /^#[\da-f]{6}$/i.test(color ?? "") ? color! : selected ? "#8ce6ad" : "#b6a4c5";
  const badge = kind === "pointer" ? "" : `<rect x="68" y="10" width="66" height="66" rx="16" fill="#170a22" fill-opacity=".88"/><g transform="translate(72 14) scale(.4)" fill="none" stroke="#fff" stroke-width="9" stroke-linecap="round" stroke-linejoin="round">${drawings[kind]}</g>`;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="144" height="144" viewBox="0 0 144 144"><image href="${brand}" width="144" height="144"/>${badge}<rect x="4" y="4" width="136" height="136" rx="28" fill="none" stroke="${marker}" stroke-width="${selected ? 5 : 2}"/><rect x="8" y="111" width="128" height="25" rx="9" fill="#100719" fill-opacity=".8"/>${selected ? '<circle cx="19" cy="20" r="8" fill="#8ce6ad"/>' : ""}</svg>`;
}

import { AbwabNode } from '../models/abwab.models';

const PATH_SEPARATOR = ' › ';

export function buildAbwabNodePaths(roots: readonly AbwabNode[]): ReadonlyMap<number, string> {
  const paths = new Map<number, string>();
  const visit = (node: AbwabNode, ancestors: readonly string[]): void => {
    const names = [...ancestors, node.name];
    paths.set(node.id, names.join(PATH_SEPARATOR));
    node.children.forEach((child) => visit(child, names));
  };
  roots.forEach((root) => visit(root, []));
  return paths;
}

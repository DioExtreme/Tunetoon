export function maskUsername(username: string): string {
  return username.slice(0, 2) + '•••';
}

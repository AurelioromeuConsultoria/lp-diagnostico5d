import { Toaster as Sonner } from "sonner";
import { useTheme } from "@/context/ThemeContext";

const Toaster = ({ ...props }) => {
  const { theme } = useTheme();
  return (
    <Sonner
      theme={theme === "dark" ? "dark" : "light"}
      className="toaster group"
      style={{
        "--normal-bg": "var(--popover)",
        "--normal-text": "var(--popover-foreground)",
        "--normal-border": "var(--border)",
      }}
      {...props}
    />
  );
};

export { Toaster }

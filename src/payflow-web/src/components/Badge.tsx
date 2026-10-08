import React from "react";

interface BadgeProps {
  status: string;
  size?: "sm" | "md";
  dot?: boolean;
  className?: string;
}

export const Badge: React.FC<BadgeProps> = ({
  status,
  size = "sm",
  dot = true,
  className = "",
}) => {
  const s = status.toLowerCase();

  // Semantic color and dot definitions
  let containerStyle = "bg-slate-50 text-slate-700 border-slate-200";
  let dotStyle = "bg-slate-400";
  let isPulsing = false;

  if (
    s === "paid" ||
    s === "approved" ||
    s === "resolved" ||
    s === "active" ||
    s === "ok" ||
    s === "present"
  ) {
    containerStyle = "bg-emerald-50/80 text-emerald-700 border-emerald-200/80";
    dotStyle = "bg-emerald-500";
  } else if (
    s === "calculating" ||
    s === "inreview" ||
    s === "pending" ||
    s === "requested"
  ) {
    containerStyle = "bg-sky-50/80 text-sky-700 border-sky-200/80";
    dotStyle = "bg-sky-500";
    isPulsing = s === "calculating";
  } else if (s === "calculated") {
    containerStyle = "bg-indigo-50/80 text-indigo-700 border-indigo-200/80";
    dotStyle = "bg-indigo-500";
  } else if (s === "draft") {
    containerStyle = "bg-amber-50/80 text-amber-700 border-amber-200/80";
    dotStyle = "bg-amber-500";
  } else if (s === "warning" || s === "haswarnings") {
    containerStyle = "bg-orange-50/80 text-orange-700 border-orange-200/80";
    dotStyle = "bg-orange-500";
  } else if (
    s === "blocking" ||
    s === "hasblockingexceptions" ||
    s === "failed" ||
    s === "rejected" ||
    s === "absent" ||
    s === "terminated"
  ) {
    containerStyle = "bg-rose-50/80 text-rose-700 border-rose-200/80";
    dotStyle = "bg-rose-500";
  } else if (s === "onleave" || s === "leave") {
    containerStyle = "bg-purple-50/80 text-purple-700 border-purple-200/80";
    dotStyle = "bg-purple-500";
  } else if (
    s === "admin" ||
    s === "hr" ||
    s === "manager" ||
    s === "accountant" ||
    s === "employee"
  ) {
    containerStyle = "bg-indigo-50/70 text-indigo-700 border-indigo-100";
    dotStyle = "bg-indigo-500";
  }

  const px = size === "sm" ? "px-2 py-0.5 text-[11px]" : "px-2.5 py-1 text-xs";

  return (
    <span
      className={`inline-flex items-center font-medium rounded-full border transition-all ${containerStyle} ${px} ${className}`}
    >
      {dot && (
        <span
          className={`w-1.5 h-1.5 rounded-full mr-1.5 shrink-0 ${dotStyle} ${
            isPulsing ? "animate-pulse" : ""
          }`}
          aria-hidden="true"
        />
      )}
      <span className="capitalize">{status}</span>
    </span>
  );
};

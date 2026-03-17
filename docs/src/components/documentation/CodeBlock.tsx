import { Terminal } from "lucide-react";

interface CodeBlockProps {
	label: string;
	children: string;
}

export const CodeBlock = ({ label, children }: CodeBlockProps) => (
	<div className="bg-slate-900 rounded-lg p-4 border border-white/10">
		<div className="flex items-center gap-2 mb-3">
			<Terminal className="w-4 h-4 text-purple-400" />
			<span className="text-sm text-slate-400">{label}</span>
		</div>
		<pre className="text-sm text-slate-300 overflow-x-auto">{children}</pre>
	</div>
);

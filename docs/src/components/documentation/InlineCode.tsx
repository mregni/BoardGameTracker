interface InlineCodeProps {
	children?: React.ReactNode;
}

export const InlineCode = ({ children }: Readonly<InlineCodeProps>) => (
	<code className="px-2 py-1 bg-slate-800 rounded text-purple-300 text-sm">
		{children}
	</code>
);

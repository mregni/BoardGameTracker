interface Props {
	comment: string | null | undefined;
}

export const SessionCommentCell = ({ comment }: Props) => {
	const text = comment?.trim();
	if (!text) return null;

	return (
		<span title={text} className="block max-w-xs truncate text-white/70">
			{text}
		</span>
	);
};

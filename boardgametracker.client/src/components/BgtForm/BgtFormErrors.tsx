interface Props {
	errors?: string[];
}

export const BgtFormErrors = ({ errors }: Props) => {
	if (!errors || errors.length === 0) {
		return null;
	}

	return (
		<div className="text-right">
			{errors.map((error) => (
				<div key={error} className="text-error text-sm">
					{error}
				</div>
			))}
		</div>
	);
};
